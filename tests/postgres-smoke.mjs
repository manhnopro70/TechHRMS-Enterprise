/**
 * Real PostgreSQL migration/provider smoke test. Test dependencies stay outside the app.
 * Usage (paths may be absolute):
 * POSTGRES_PACKAGE=/path/node_modules/embedded-postgres/dist/index.js \
 * BASELINE_PROJECT=/path/to/v3 DOTNET_PATH=dotnet node tests/postgres-smoke.mjs
 * The baseline and current backend must be built in Release before invoking.
 * Creates only fresh randomly named local test databases. Never reads production configuration.
 */
import assert from 'node:assert/strict';
import { mkdtemp, mkdir, readFile, writeFile, open } from 'node:fs/promises';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { tmpdir } from 'node:os';
import { randomBytes } from 'node:crypto';
import { spawn } from 'node:child_process';
import { setTimeout as delay } from 'node:timers/promises';

const repo = path.resolve(path.dirname(new URL(import.meta.url).pathname), '..');
const packagePath = process.env.POSTGRES_PACKAGE;
const baseline = process.env.BASELINE_PROJECT;
if (!packagePath || !baseline) throw new Error('POSTGRES_PACKAGE and BASELINE_PROJECT must point to local test-only dependencies/source.');
const { default: EmbeddedPostgres } = await import(pathToFileURL(path.resolve(packagePath)).href);
const dotnet = process.env.DOTNET_PATH || 'dotnet';
const work = await mkdtemp(path.join(tmpdir(), 'techhrms-pg-test-'));
const password = randomBytes(24).toString('hex');
const pgPort = 55432; const apiPort = 5278;
const baseUrl = 'http://127.0.0.1:' + apiPort;
const observations = [];
const result = { database: 'PostgreSQL', baseline: path.basename(baseline), passed: false, checks: observations };
let server;
let pgStarted = false;
let sequence = 0;
const pgLog = [];
const pg = new EmbeddedPostgres({ databaseDir: path.join(work, 'database'), user: 'postgres', password,
  port: pgPort, persistent: true, autoRehydrateSymlinks: true, createPostgresUser: false, postgresFlags: ['-k', ''],
  onLog: message => pgLog.push(String(message)), onError: message => pgLog.push(String(message)) });
async function stopApi() {
  if (!server) return;
  const target = server; server = undefined;
  if (target.exitCode === null) {
    target.kill('SIGTERM');
    for (let i=0;i<50 && target.exitCode===null;i++) await delay(100);
    if (target.exitCode === null) target.kill('SIGKILL');
  }
  await target.log.close();
}
async function startApi(project, database) {
  const logfile = path.join(work, 'api-' + (++sequence) + '.log');
  const log = await open(logfile,'w');
  const dll = path.join(project,'backend/bin/Release/net10.0/TechHRMS.Api.dll');
  const child = spawn(dotnet,[dll,'--urls',baseUrl], {cwd:path.join(project,'backend'), stdio:['ignore',log.fd,log.fd], env:{...process.env,
    ASPNETCORE_ENVIRONMENT:'Development', ASPNETCORE_URLS:baseUrl,
    ConnectionStrings__Postgres:'Host=127.0.0.1;Port='+pgPort+';Database='+database+';Username=postgres;Password='+password+';SSL Mode=Disable',
    SEED_ADMIN_EMAIL:'admin@techhrms.local', SEED_ADMIN_PASSWORD:'LocalDemo@2026!', SEED_SAMPLE_DATA:'true',
    HRMS_TIME_ZONE:'Asia/Ho_Chi_Minh' }});
  child.log = log; server = child;
  for (let i=0;i<300;i++) {
    if (child.exitCode !== null) throw new Error('API exited: '+(await readFile(logfile,'utf8')).slice(-8000));
    try { const response=await fetch(baseUrl+'/api/health/ready'); if(response.ok) return; } catch {}
    await delay(100);
  }
  throw new Error('API startup timeout: '+(await readFile(logfile,'utf8')).slice(-8000));
}
function client() {
  const cookies = new Map(); let token;
  async function request(method,route,body,expected=200) {
    const headers = {Cookie:[...cookies].map(([key,value])=>key+'='+value).join('; ')};
    if (body!==undefined) headers['Content-Type']='application/json';
    if (token && !['GET','HEAD'].includes(method)) headers['X-CSRF-TOKEN']=token;
    const response = await fetch(baseUrl+'/api'+route,{method,headers,body:body===undefined?undefined:JSON.stringify(body),redirect:'manual'});
    for (const entry of response.headers.getSetCookie()) {
      const first=entry.split(';')[0];const pos=first.indexOf('=');cookies.set(first.slice(0,pos),first.slice(pos+1));
    }
    const text=await response.text();let json;try{json=JSON.parse(text)}catch{json=text}
    assert.equal(response.status,expected,method+' '+route+' => '+response.status+' '+JSON.stringify(json));
    return json;
  }
  return {request, async login(email='admin@techhrms.local') {
    token=(await request('GET','/auth/csrf')).token;
    const session=await request('POST','/auth/login',{email,password:'LocalDemo@2026!'});
    token=(await request('GET','/auth/csrf')).token;return session;
  }};
}
async function snapshot(database) {
  const sql=pg.getPgClient(database);await sql.connect();
  try {
    const accounts=await sql.query('SELECT "Id","EmployeeId","Email","PasswordHash","Role","IsActive" FROM "Accounts" ORDER BY "Id"');
    const employees=await sql.query('SELECT * FROM "Employees" ORDER BY "Id"');
    return {accounts:accounts.rows,employees:employees.rows};
  } finally {await sql.end()}
}
async function sqlQuery(database,query) {
  const sql=pg.getPgClient(database);await sql.connect();try{return await sql.query(query)}finally{await sql.end()}
}
try {
  await pg.initialise(); await pg.start(); pgStarted=true;
  await pg.createDatabase('hrm_upgrade'); await pg.createDatabase('hrm_fresh');
  await startApi(baseline,'hrm_upgrade');
  const before=await snapshot('hrm_upgrade');
  assert.equal(before.accounts.length,4);assert.equal(before.employees.length,8);
  await stopApi();
  observations.push('V3 created a real PostgreSQL database with 8 employees and 4 accounts.');
  await startApi(repo,'hrm_upgrade');
  assert.deepEqual(await snapshot('hrm_upgrade'),before);
  observations.push('V3 → V4 preserved all employee rows, account IDs, emails, roles and password hashes.');
  const admin=client();const adminSession=await admin.login();
  assert.equal(adminSession.role,'Admin');
  const employee=client();const employeeSession=await employee.login('employee@techhrms.local');
  const manager=client();await manager.login('manager@techhrms.local');
  for (const route of ['/employees','/requests','/leave-balances','/notifications','/timesheets','/payroll/periods','/offboarding','/candidates','/contracts','/attendance','/courses','/insights']) await admin.request('GET',route);
  observations.push('PostgreSQL queries for all new modules and original operational lists completed.');
  const leave=(await employee.request('POST','/requests',{type:'Leave',startDate:'2050-01-03',endDate:'2050-01-03',reason:'Nghỉ phép kiểm thử PostgreSQL'})).id;
  const balances=await employee.request('GET','/leave-balances?year=2050');assert.equal(balances[0].pending,1);assert.equal(balances[0].available,11);
  await employee.request('POST','/requests/'+leave+'/cancel');
  const approvedLeave=(await employee.request('POST','/requests',{type:'Leave',startDate:'2050-01-04',endDate:'2050-01-04',reason:'Kiểm tra phê duyệt PostgreSQL'})).id;
  await manager.request('POST','/requests/'+approvedLeave+'/decision',{status:'Approved',note:'Đã kiểm tra'});
  const afterLeave=await employee.request('GET','/leave-balances?year=2050');assert.equal(afterLeave[0].used,1);assert.equal(afterLeave[0].pending,0);
  observations.push('Leave reserve/cancel/approve and balance calculations verified on PostgreSQL.');
  const projects=await employee.request('GET','/projects');assert(projects.length>0);
  const workDate=new Intl.DateTimeFormat('en-CA',{timeZone:'Asia/Ho_Chi_Minh',year:'numeric',month:'2-digit',day:'2-digit'}).format(new Date());
  const timesheet=(await employee.request('POST','/timesheets',{projectId:projects[0].id,workDate,hours:7.5,billable:true,note:'Kiểm thử giờ công PostgreSQL'})).id;
  await employee.request('POST','/timesheets/'+timesheet+'/submit');
  await manager.request('POST','/timesheets/'+timesheet+'/decision',{status:'Approved',note:''});
  const sheets=await employee.request('GET','/timesheets?month='+workDate.slice(0,7));assert(sheets.some(x=>x.id===timesheet&&x.status==='Approved'&&x.hours===7.5));
  observations.push('Decimal timesheet persistence, assignment validation and manager approval verified.');
  const period=(await admin.request('POST','/payroll/periods',{month:'2050-01',title:'Kỳ lương kiểm thử'})).id;
  await admin.request('PUT','/payroll/periods/'+period+'/items/'+employeeSession.id,{baseSalary:10000000,allowances:2000000,bonus:500000,deductions:1500000,note:'Dữ liệu kiểm thử'});
  await admin.request('POST','/payroll/periods/'+period+'/approve');await admin.request('POST','/payroll/periods/'+period+'/publish');
  const payslips=await employee.request('GET','/payslips');assert.equal(payslips.length,1);assert.equal(payslips[0].net,11000000);
  await manager.request('GET','/payslips/'+payslips[0].id,undefined,404);
  await admin.request('PUT','/payroll/periods/'+period+'/items/'+employeeSession.id,{baseSalary:1,allowances:0,bonus:0,deductions:0,note:''},409);
  const notifications=await employee.request('GET','/notifications');assert(notifications.unreadCount>=3);
  observations.push('Payroll arithmetic, published immutability, own-only payslip access and notifications verified.');
  const department=(await admin.request('GET','/departments'))[0].id;
  const candidate=(await admin.request('POST','/candidates',{fullName:'Nhân viên kiểm thử PostgreSQL',email:'pg-hire@example.test',position:'Backend Developer'})).id;
  await admin.request('PUT','/candidates/'+candidate+'/stage',{stage:'Offer'});
  const hired=await admin.request('POST','/candidates/'+candidate+'/hire',{departmentId:department,jobTitle:'Backend Developer',workMode:'Hybrid'});
  await admin.request('POST','/candidates/'+candidate+'/hire',{departmentId:department,jobTitle:'Backend Developer',workMode:'Hybrid'},409);
  const offboarding=(await admin.request('POST','/offboarding',{employeeId:hired.employeeId,lastWorkingDate:workDate,reason:'Bàn giao trong bài kiểm thử'})).id;
  const cases=await admin.request('GET','/offboarding');
  for(const task of cases.find(x=>x.id===offboarding).checklist) await admin.request('PUT','/offboarding/'+offboarding+'/tasks/'+task.id,{done:true});
  await admin.request('POST','/offboarding/'+offboarding+'/complete');
  const people=await admin.request('GET','/employees');assert.equal(people.find(x=>x.id===hired.employeeId).status,'Inactive');
  observations.push('ATS hire creates onboarding once; offboarding completes atomically and deactivates employee.');
  const beforeRestart=await snapshot('hrm_upgrade');
  await stopApi();await startApi(repo,'hrm_upgrade');
  assert.deepEqual(await snapshot('hrm_upgrade'),beforeRestart);
  assert.equal((await admin.request('GET','/auth/me')).role,'Admin');
  const keyCount=await sqlQuery('hrm_upgrade','SELECT COUNT(*)::int AS count FROM "DataProtectionKeys"');assert(keyCount.rows[0].count>0);
  observations.push('Second startup idempotent; persisted cookie remains valid after app restart and keys stored in database.');
  await stopApi();await startApi(repo,'hrm_fresh');
  const fresh=client();await fresh.login();assert.equal((await fresh.request('GET','/employees')).length,8);
  await fresh.request('POST','/payroll/periods',{month:'2050-02',title:'Kiểm thử cơ sở mới'});
  observations.push('Fresh V4 PostgreSQL bootstrap, login and operational write verified.');
  result.passed=true;
} catch(error) {
  result.error=String(error?.stack || error);process.exitCode=1;
} finally {
  await stopApi();if(pgStarted) await pg.stop();
  await writeFile(path.join(work,'postgres.log'),pgLog.join('\n'));
  await mkdir(path.join(repo,'tests/results'),{recursive:true});
  await writeFile(path.join(repo,'tests/results/postgres-smoke.json'),JSON.stringify(result,null,2)+'\n');
  console.log(JSON.stringify(result,null,2));console.log('Private diagnostic files: '+work);
}
