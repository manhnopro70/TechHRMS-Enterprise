"""Real HTTP integration checks. Creates disposable LOCAL databases only; never uses Render.

Usage: python tests/run_integration.py --dotnet dotnet
Optional --postgres accepts a disposable localhost PostgreSQL connection string.
The test runner starts/stops its own API. No external Python packages required.
"""
from __future__ import annotations
import argparse
import base64
import copy
import datetime as dt
import hashlib
import http.cookiejar
import json
import os
from pathlib import Path
import socket
import sqlite3
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
from concurrent.futures import ThreadPoolExecutor

ROOT = Path(__file__).resolve().parents[1]
DEMO_PASSWORD = "LocalDemo@2026!"
TEMP_PASSWORD = "Temporary@2026!"
STAFF_PASSWORD = "ChangedLogin@2026!"
checks: list[str] = []


def check(ok, label):
    if not ok:
        raise AssertionError(label)
    checks.append(label)


class Client:
    def __init__(self, base, cookies=None):
        self.base = base
        self.cookies = cookies or http.cookiejar.CookieJar()
        self.opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), urllib.request.HTTPCookieProcessor(self.cookies))

    def clone(self):
        jar = http.cookiejar.CookieJar()
        for cookie in self.cookies:
            jar.set_cookie(copy.copy(cookie))
        return Client(self.base, jar)

    def call(self, method, path, data=None, expected=200, csrf=True):
        headers = {"Content-Type": "application/json", "Origin": self.base}
        if method not in ("GET", "HEAD") and csrf:
            headers["X-CSRF-TOKEN"] = self.call("GET", "/auth/csrf")["token"]
        raw = None if data is None else json.dumps(data).encode()
        request = urllib.request.Request(self.base + "/api" + path, data=raw, headers=headers, method=method)
        try:
            with self.opener.open(request, timeout=20) as response:
                status = response.status
                payload = response.read().decode()
        except urllib.error.HTTPError as response:
            status = response.code
            payload = response.read().decode()
        try:
            value = json.loads(payload) if payload else None
        except json.JSONDecodeError:
            value = payload[:400]
        if expected is not None:
            accepted = [expected] if isinstance(expected, int) else expected
            if status not in accepted:
                raise AssertionError(f"{method} {path}: expected {accepted}, got {status}: {value}")
            if path != "/auth/csrf":
                checks.append(f"{method} {path} -> {status}")
        return (status, value) if expected is None else value

    def login(self, email, password=DEMO_PASSWORD):
        return self.call("POST", "/auth/login", {"email": email, "password": password})


class Server:
    def __init__(self, dotnet, database, log_file, postgres=None):
        self.dotnet = dotnet
        self.database = database
        self.log_file = log_file
        self.postgres = postgres
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            self.port = sock.getsockname()[1]
        self.base = f"http://127.0.0.1:{self.port}"
        self.process = None
        self.output = None

    def start(self):
        env = os.environ.copy()
        for key in list(env):
            if key.lower().startswith("connectionstrings") or key.startswith("SEED_"):
                del env[key]
        env.update(ASPNETCORE_ENVIRONMENT="Development", Logging__LogLevel__Default="Warning",
                   Logging__LogLevel__Microsoft="Warning", SEED_ADMIN_EMAIL="admin@techhrms.local",
                   SEED_ADMIN_PASSWORD=DEMO_PASSWORD, HRMS_TIME_ZONE="Asia/Ho_Chi_Minh")
        if self.postgres:
            env["ConnectionStrings__Postgres"] = self.postgres
        else:
            env["ConnectionStrings__Sqlite"] = "Data Source=" + str(self.database)
        self.output = open(self.log_file, "a", encoding="utf-8")
        self.process = subprocess.Popen([self.dotnet, str(ROOT / "backend/bin/Release/net10.0/TechHRMS.Api.dll"),
                                         "--urls", self.base], cwd=ROOT / "backend", env=env,
                                        stdout=self.output, stderr=subprocess.STDOUT)
        opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
        for _ in range(200):
            if self.process.poll() is not None:
                raise RuntimeError("API exited during startup. See " + str(self.log_file))
            try:
                with opener.open(self.base + "/api/health/ready", timeout=1) as response:
                    if response.status == 200:
                        return
            except (OSError, urllib.error.URLError):
                time.sleep(0.15)
        raise RuntimeError("API did not become ready. See " + str(self.log_file))

    def stop(self):
        if self.process is not None and self.process.poll() is None:
            self.process.terminate()
            try:
                self.process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                self.process.kill()
                self.process.wait()
        if self.output:
            self.output.close()


def legacy_database(path):
    con = sqlite3.connect(path)
    con.executescript((ROOT / "tests/fixtures/legacy-v31-sqlite.sql").read_text())
    salt = os.urandom(16)
    digest = hashlib.pbkdf2_hmac("sha256", DEMO_PASSWORD.encode(), salt, 210_000, dklen=32)
    old_hash = "pbkdf2_sha256$210000$" + base64.b64encode(salt).decode() + "$" + base64.b64encode(digest).decode()
    con.execute('INSERT INTO Departments(Id,Name,Code) VALUES(1,?,?)', ("Đơn vị giữ lại", "LEGACY"))
    con.execute('INSERT INTO Employees(Id,FullName,Email,JobTitle,DepartmentId,WorkMode,Status,IsBench,JoinedAt) VALUES(1,?,?,?,?,?,?,?,?)',
                ("Quản trị giữ lại", "admin@techhrms.local", "Administrator", 1, "Hybrid", "Active", 0, "2026-01-01"))
    con.execute('INSERT INTO Accounts(Id,EmployeeId,Email,PasswordHash,Role,IsActive) VALUES(1,1,?,?,?,1)',
                ("admin@techhrms.local", old_hash, "Admin"))
    con.commit()
    con.close()
    return old_hash


def suite(server):
    anon = Client(server.base)
    anon.call("GET", "/employees", expected=401)
    anon.call("POST", "/auth/login", {"email": "admin@techhrms.local", "password": DEMO_PASSWORD}, expected=403, csrf=False)
    anon.call("POST", "/auth/login", {"email": "admin@techhrms.local", "password": "wrong-password"}, expected=401)
    admin = Client(server.base)
    session = admin.login("admin@techhrms.local")
    check(session["role"] == "Admin", "existing admin still logs in")
    admin.call("GET", "/does-not-exist", expected=404)
    admin.call("GET", "/dashboard")
    accounts = admin.call("GET", "/accounts")
    check("passwordHash" not in json.dumps(accounts) and "securityStamp" not in json.dumps(accounts), "account DTO excludes password/stamp")
    admin_account = next(a["id"] for a in accounts if a["employeeId"] == session["id"])
    admin.call("PUT", f"/accounts/{admin_account}", {"role": "Employee", "isActive": True}, expected=400)
    admin.call("PUT", f"/accounts/{admin_account}", {"role": "Admin", "isActive": False}, expected=400)
    departments_before = admin.call("GET", "/departments")
    admin.call("POST", "/departments", {"name": "Đội kiểm thử", "code": "TESTA"})
    admin.call("POST", "/departments", {"name": "Đơn vị ngoài phạm vi", "code": "TESTB"})
    departments = admin.call("GET", "/departments")
    dept = next(d["id"] for d in departments if d["code"] == "TESTA")
    outside_dept = next(d["id"] for d in departments if d["code"] == "TESTB")
    check(len(departments) == len(departments_before) + 2, "department persistence")

    def provision(label, role="Employee", department=dept):
        email = label + "@integration.test"
        employee = {"fullName": "Kiểm thử " + label, "email": email, "jobTitle": "Engineer", "departmentId": department,
                    "workMode": "Hybrid", "status": "Active", "isBench": True,
                    "account": {"role": role, "password": TEMP_PASSWORD}}
        result = admin.call("POST", "/employees", employee)
        client = Client(server.base)
        login = client.login(email, TEMP_PASSWORD)
        check(login["mustChangePassword"] is True, label + " forced first password change")
        client.call("GET", "/dashboard", expected=403)
        changed = client.call("POST", "/auth/change-password", {"currentPassword": TEMP_PASSWORD, "newPassword": STAFF_PASSWORD})
        check(changed["mustChangePassword"] is False, label + " password gate cleared")
        return result["id"], client, email

    employee_id, employee, employee_email = provision("employee")
    manager_id, manager, _ = provision("manager", "Manager")
    hr_id, hr, _ = provision("hr", "HR")
    outside_id, outside, _ = provision("outside", department=outside_dept)
    employee.call("GET", "/accounts", expected=403)
    hr.call("GET", "/accounts", expected=403)
    manager.call("GET", "/accounts", expected=403)
    employee.call("POST", "/employees", {"fullName": "Forbidden", "email": "no@integration.test", "jobTitle": "Dev", "departmentId": dept,
                                            "workMode": "Hybrid", "status": "Active", "isBench": False}, expected=403)
    check([x["id"] for x in employee.call("GET", "/employees")] == [employee_id], "Employee list scoped to self")
    check(outside_id not in [x["id"] for x in manager.call("GET", "/employees")], "Manager list excludes other department")
    before = len(admin.call("GET", "/employees"))
    hr.call("POST", "/employees", {"fullName": "No escalation", "email": "escalate@integration.test", "jobTitle": "Dev", "departmentId": dept,
                                     "workMode": "Hybrid", "status": "Active", "isBench": False,
                                     "account": {"role": "Admin", "password": TEMP_PASSWORD}}, expected=403)
    check(len(admin.call("GET", "/employees")) == before, "rejected account provision leaves no orphan employee")
    admin.call("POST", "/accounts", {"employeeId": employee_id, "role": "Employee", "password": TEMP_PASSWORD}, expected=409)
    plain = admin.call("POST", "/employees", {"fullName": "Chỉ hồ sơ", "email": "profile@integration.test", "jobTitle": "Tester", "departmentId": dept,
                                                "workMode": "Onsite", "status": "Active", "isBench": False})
    admin.call("POST", "/accounts", {"employeeId": plain["id"], "role": "Employee", "password": "short"}, expected=400)
    admin.call("POST", "/accounts", {"employeeId": plain["id"], "role": "Employee", "password": TEMP_PASSWORD})
    employee.call("PUT", "/auth/profile", {"fullName": "Nhân viên đã cập nhật"})
    check(employee.call("GET", "/auth/profile")["fullName"] == "Nhân viên đã cập nhật", "self profile edit persisted")

    # A locked/reset account loses existing sessions even after being re-enabled.
    employee_account = next(a["id"] for a in admin.call("GET", "/accounts") if a["employeeId"] == employee_id)
    old_session = employee.clone()
    admin.call("PUT", f"/accounts/{employee_account}", {"role": "Employee", "isActive": False})
    employee.call("GET", "/auth/me", expected=401)
    admin.call("PUT", f"/accounts/{employee_account}", {"role": "Employee", "isActive": True})
    old_session.call("GET", "/auth/me", expected=401)
    employee.login(employee_email, STAFF_PASSWORD)
    old_session = employee.clone()
    admin.call("POST", f"/accounts/{employee_account}/reset-password", {"password": TEMP_PASSWORD})
    old_session.call("GET", "/employees", expected=401)
    employee.login(employee_email, TEMP_PASSWORD)
    employee.call("GET", "/employees", expected=403)
    employee.call("POST", "/auth/change-password", {"currentPassword": "wrong-password", "newPassword": STAFF_PASSWORD}, expected=400)
    employee.call("POST", "/auth/change-password", {"currentPassword": TEMP_PASSWORD, "newPassword": STAFF_PASSWORD})

    # Persistent keys keep valid cookies usable after an API restart; tables upgrade idempotently.
    server.stop()
    server.start()
    check(employee.call("GET", "/auth/me")["id"] == employee_id, "session survives server restart")
    check(admin.call("GET", "/auth/me")["role"] == "Admin", "admin session survives restart")

    year = dt.date.today().year
    monday = dt.date(year, 1, 1)
    while monday.weekday() != 0:
        monday += dt.timedelta(days=1)
    date = monday.isoformat()
    admin.call("PUT", f"/leave-balances/{employee_id}/{year}", {"entitlement": 3})
    leave = employee.call("POST", "/requests", {"type": "Leave", "startDate": date, "endDate": date, "reason": "Nghỉ phép kiểm thử"})["id"]
    balance = employee.call("GET", f"/leave-balances?year={year}")[0]
    check(balance["pending"] == 1 and balance["available"] == 2, "pending leave reserves allowance")
    employee.call("POST", "/requests", {"type": "WFH", "startDate": date, "endDate": date, "reason": "Ngày làm tại nhà"}, expected=409)
    admin.call("PUT", f"/leave-balances/{employee_id}/{year}", {"entitlement": 0}, expected=400)
    employee.call("POST", f"/requests/{leave}/decision", {"status": "Approved", "note": ""}, expected=403)
    manager.call("POST", f"/requests/{leave}/decision", {"status": "Rejected", "note": ""}, expected=400)
    manager.call("POST", f"/requests/{leave}/decision", {"status": "Approved", "note": "Đồng ý"})
    manager.call("POST", f"/requests/{leave}/decision", {"status": "Approved", "note": "Đồng ý"}, expected=409)
    check(employee.call("GET", f"/leave-balances?year={year}")[0]["used"] == 1, "approval counts used leave once")
    d2 = (monday + dt.timedelta(days=1)).isoformat()
    cancel = employee.call("POST", "/requests", {"type": "Leave", "startDate": d2, "endDate": d2, "reason": "Đơn phép sẽ rút"})["id"]
    employee.call("POST", f"/requests/{cancel}/cancel")
    check(employee.call("GET", f"/leave-balances?year={year}")[0]["pending"] == 0, "cancel releases pending allowance")
    out = outside.call("POST", "/requests", {"type": "WFH", "startDate": d2, "endDate": d2, "reason": "Làm việc ngoài văn phòng"})["id"]
    manager.call("POST", f"/requests/{out}/decision", {"status": "Approved"}, expected=403)
    own = manager.call("POST", "/requests", {"type": "WFH", "startDate": d2, "endDate": d2, "reason": "Làm việc từ xa"})["id"]
    manager.call("POST", f"/requests/{own}/decision", {"status": "Approved"}, expected=400)
    employee.call("POST", "/requests", {"type": "OT", "startDate": date, "endDate": date, "reason": "Ngoài giờ kiểm thử", "hours": 13}, expected=400)
    employee.call("POST", "/requests", {"type": "OT", "startDate": d2, "endDate": d2, "reason": "Ngoài giờ kiểm thử", "hours": 2})
    notifications = employee.call("GET", "/notifications")
    check(notifications["unreadCount"] > 0, "decision created persisted notification")
    notice_id = notifications["items"][0]["id"]
    outside.call("PUT", f"/notifications/{notice_id}/read", expected=404)
    employee.call("PUT", "/notifications/read-all")
    check(employee.call("GET", "/notifications")["unreadCount"] == 0, "notifications mark-all-read")

    # Duplicate creation and two concurrent decisions cannot double spend leave.
    d3 = (monday + dt.timedelta(days=2)).isoformat()
    concurrent_leave = employee.call("POST", "/requests", {"type": "Leave", "startDate": d3, "endDate": d3, "reason": "Duyệt đồng thời"})["id"]
    twins = [manager.clone(), manager.clone()]
    with ThreadPoolExecutor(max_workers=2) as pool:
        codes = list(pool.map(lambda c: c.call("POST", f"/requests/{concurrent_leave}/decision", {"status": "Approved"}, expected=None)[0], twins))
    check(sorted(codes) == [200, 409], "concurrent decisions have exactly one successful transition")

    project = admin.call("POST", "/projects", {"code": "INT-PROJ", "name": "Dự án kiểm thử"})["id"]
    project2 = admin.call("POST", "/projects", {"code": "INT-PROJ2", "name": "Dự án kiểm thử khác"})["id"]
    admin.call("POST", "/allocations", {"employeeId": employee_id, "techProjectId": project, "percent": 50})
    admin.call("POST", "/allocations", {"employeeId": employee_id, "techProjectId": project2, "percent": 50})
    today = dt.date.today().isoformat()
    body = {"projectId": project, "workDate": today, "hours": 8, "billable": True, "note": "Phát triển chức năng"}
    timesheet = employee.call("POST", "/timesheets", body)["id"]
    employee.call("POST", "/timesheets", body, expected=400)
    employee.call("POST", "/timesheets", {**body, "projectId": project2, "hours": 17}, expected=400)
    outside.call("POST", "/timesheets", body, expected=400)
    employee.call("POST", f"/timesheets/{timesheet}/submit")
    employee.call("PUT", f"/timesheets/{timesheet}", body, expected=409)
    manager.call("POST", f"/timesheets/{timesheet}/decision", {"status": "Approved", "note": "Đã kiểm tra"})
    employee.call("DELETE", f"/timesheets/{timesheet}", expected=409)
    check(outside.call("GET", "/timesheets") == [], "outside employee cannot read project timesheets")

    # Clock records and existing V3 modules remain usable.
    employee.call("POST", "/attendance/check-in", {"mode": "Hybrid", "note": "Kiểm thử chấm công"})
    employee.call("POST", "/attendance/check-in", {"mode": "Hybrid", "note": "Trùng"}, expected=[400, 409])
    employee.call("POST", "/attendance/check-out", {})
    employee.call("POST", "/skills", {"employeeId": employee_id, "name": "TypeScript", "level": "Advanced", "certificate": "Demo"})
    skills = employee.call("GET", "/skills")
    check(any(s["name"] == "TypeScript" for s in skills), "skill saved")
    employee.call("POST", "/objectives", {"employeeId": employee_id, "title": "Hoàn thiện ứng dụng demo", "quarter": "2026-Q4"})
    goal = employee.call("GET", "/objectives")[0]["id"]
    employee.call("PUT", f"/objectives/{goal}/progress", {"progress": 65})
    employee.call("PUT", f"/objectives/{goal}/progress", {"progress": 101}, expected=400)
    for endpoint in ["/organization", "/contracts", "/onboarding", "/courses", "/enrollments", "/assets", "/attendance"]:
        employee.call("GET", endpoint)
    admin.call("GET", "/insights")

    month = dt.date.today().strftime("%Y-%m")
    period = hr.call("POST", "/payroll/periods", {"month": month, "title": "Bảng lương kiểm thử"})["id"]
    manager.call("GET", "/payroll/periods", expected=403)
    employee.call("GET", f"/payroll/periods/{period}/items", expected=403)
    money = {"baseSalary": 20_000_000, "allowances": 1_000_000, "bonus": 2_000_000, "deductions": 3_000_000, "note": "Dữ liệu kiểm thử"}
    hr.call("PUT", f"/payroll/periods/{period}/items/{employee_id}", money)
    hr.call("PUT", f"/payroll/periods/{period}/items/{outside_id}", {**money, "baseSalary": 10_000_000})
    items = hr.call("GET", f"/payroll/periods/{period}/items")
    mine = next(x for x in items if x["employeeId"] == employee_id)
    check(mine["gross"] == 23_000_000 and mine["net"] == 20_000_000, "payroll gross/net exact arithmetic")
    employee.call("GET", f"/payslips/{mine['id']}", expected=404)
    hr.call("POST", f"/payroll/periods/{period}/approve")
    hr.call("POST", f"/payroll/periods/{period}/publish", expected=403)
    admin.call("POST", f"/payroll/periods/{period}/publish")
    hr.call("PUT", f"/payroll/periods/{period}/items/{employee_id}", money, expected=409)
    admin.call("POST", f"/payroll/periods/{period}/reopen", expected=409)
    check(len(employee.call("GET", "/payslips")) == 1, "employee sees only own published payslip")
    employee.call("GET", f"/payslips/{mine['id']}")
    outside.call("GET", f"/payslips/{mine['id']}", expected=404)
    manager.call("GET", f"/payslips/{mine['id']}", expected=404)

    candidate = hr.call("POST", "/candidates", {"fullName": "Ứng viên kiểm thử", "email": "candidate@integration.test", "position": "Engineer"})["id"]
    hr.call("PUT", f"/candidates/{candidate}/stage", {"stage": "Offer"})
    hire = hr.call("POST", f"/candidates/{candidate}/hire", {"departmentId": dept, "jobTitle": "Engineer", "workMode": "Hybrid"})
    check(hire["employeeId"] > 0, "ATS created employee")
    hr.call("POST", f"/candidates/{candidate}/hire", {"departmentId": dept, "jobTitle": "Engineer", "workMode": "Hybrid"}, expected=409)
    check(len([t for t in hr.call("GET", "/onboarding") if t["employeeId"] == hire["employeeId"]]) == 3, "ATS creates onboarding checklist")

    asset = hr.call("POST", "/assets", {"assetTag": "INT-LAPTOP", "name": "Máy tính kiểm thử", "assignedToEmployeeId": employee_id})["id"]
    off = hr.call("POST", "/offboarding", {"employeeId": employee_id, "lastWorkingDate": today, "reason": "Hoàn thành thử nghiệm"})["id"]
    hr.call("POST", f"/offboarding/{off}/complete", expected=400)
    case = next(x for x in hr.call("GET", "/offboarding") if x["id"] == off)
    for task in case["checklist"]:
        hr.call("PUT", f"/offboarding/{off}/tasks/{task['id']}", {"done": True})
    hr.call("POST", f"/offboarding/{off}/complete")
    employee.call("GET", "/auth/me", expected=401)
    check(next(x for x in hr.call("GET", "/assets") if x["id"] == asset)["assignedToEmployeeId"] is None, "offboarding reclaims assigned device")
    check(not any(x["employeeId"] == employee_id for x in hr.call("GET", "/allocations")), "offboarding releases allocations")
    check(len(admin.call("GET", "/payslips")) == 2, "offboarding preserves historic payslips")
    audit = admin.call("GET", "/audit")
    check(len(audit) > 20 and TEMP_PASSWORD not in json.dumps(audit) and STAFF_PASSWORD not in json.dumps(audit), "audit populated without passwords")
    print("PASS account lifecycle, RBAC, CSRF, sessions, leave, notifications, timesheets, payroll, ATS, offboarding", flush=True)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--no-build", action="store_true")
    parser.add_argument("--postgres", default=None, help="Disposable localhost database only")
    parser.add_argument("--only", choices=["fresh", "legacy", "both"], default="both")
    args = parser.parse_args()
    if args.postgres and not any(v in args.postgres.lower() for v in ["host=127.0.0.1;", "host=localhost;"]):
        parser.error("Refusing non-local PostgreSQL connection. These tests must not run against production.")
    if not args.no_build:
        subprocess.run([args.dotnet, "build", str(ROOT / "backend/TechHRMS.Api.csproj"), "-c", "Release", "--nologo"], check=True)
    results_dir = ROOT / "test-results"
    results_dir.mkdir(exist_ok=True)
    modes = ["postgres"] if args.postgres else (["fresh", "legacy"] if args.only == "both" else [args.only])
    for mode in modes:
        with tempfile.TemporaryDirectory(prefix="techhrms-test-") as tmp:
            db = Path(tmp) / "integration.db"
            old_hash = legacy_database(db) if mode == "legacy" else None
            server = Server(args.dotnet, db, results_dir / (mode + ".log"), args.postgres)
            try:
                server.start()
                if old_hash:
                    con = sqlite3.connect(db)
                    check(con.execute("SELECT PasswordHash FROM Accounts WHERE Id=1").fetchone()[0] == old_hash, "legacy migration preserved password hash")
                    check(con.execute("SELECT FullName FROM Employees WHERE Id=1").fetchone()[0] == "Quản trị giữ lại", "legacy migration preserved employee")
                    con.close()
                before = len(checks)
                suite(server)
                print(f"PASS {mode}: {len(checks)-before} checks", flush=True)
            finally:
                server.stop()
    report = {"utc": dt.datetime.now(dt.timezone.utc).isoformat(), "checksPassed": len(checks), "modes": modes, "checks": checks}
    (results_dir / "integration-results.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"TOTAL: {len(checks)} checks passed", flush=True)


if __name__ == "__main__":
    main()
