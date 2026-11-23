# API V4 – workflow contract

All endpoints below prefixed `/api`, cookie authenticated, mutation requires `X-CSRF-TOKEN` from GET `/auth/csrf`. JSON fields camelCase. Bad input400, forbidden403, missing404, state/duplicate/concurrent conflict409. Auth+accounts documented by auth owner separately.

## Notifications

- GET `/notifications` => `{items:[{id,title,message,link,isRead,createdAtUtc}],unreadCount}`. Own only, latest200 unread-first. `link` is frontend Page key (`requests`, `timesheets`, `payroll`, `offboarding`).
- PUT `/notifications/{id}/read`, PUT `/notifications/read-all` empty body; own only.

## Annual leave and requests

- GET `/leave-balances?year=2026&employeeId=3` => `[{employeeId,employee,year,entitlement,used,pending,available}]`; optional parameters, scoped own / manager dept / HRall. Default demo entitlement12 days unless configured. `available=entitlement-used-pending`.
- PUT `/leave-balances/{employeeId}/{year}` `{entitlement:20}` HR/Admin; integer0..366, cannot lower below used+reserved.
- GET `/requests` existing fields + `hours:number|null`, `days:int`.
- POST `/requests` `{type:'Leave'|'WFH'|'OT',startDate:'yyyy-MM-dd',endDate:'...',reason:'5..500chars',hours?:number}` => `{id}`. Leave weekdays Monday–Friday only, same calendar year, >0 workingdays, <=46calendar days. Years2000..2100. OT one day, hours>0<=12, 2decimal precision; no hours allowed other types. Reject active pending/approved date overlaps same type or any Leave/WFH overlap. Own only; reserves Pending leave immediately. Holidays/halfday/carryover not implemented.
- POST `/requests/{id}/decision` `{status:'Approved'|'Rejected',note?:string}`. Manager own department / HRall, never own request. Reject note3..300 required. Only Pending, remaining leavechecked again.
- POST `/requests/{id}/cancel` no body, own Pending only. Releases reservation by changing to Cancelled. Repeated decision/cancel409.

## Project timesheets

- GET `/timesheets?month=2026-11` => `[{id,employeeId,employee,projectId,project,workDate,hours,billable,note,status,reviewNote}]`, same scope own/dept/all.
- POST `/timesheets` / PUT `/timesheets/{id}` `{projectId,workDate:'yyyy-MM-dd',hours:8,billable:true,note:'3..500chars'}` => `{id}`. Own only, current allocation to Active project needed; date2000..today in configured timezone; >0..24hours with2decimals; totalallprojects/day<=24. Unique employee+project+date. Draft or Rejected can edit; edited stateDraft.
- DELETE `/timesheets/{id}` own Draft/Rejected only.
- POST `/timesheets/{id}/submit` own Draft/Rejected -> Submitted; validates allocation/hours again.
- POST `/timesheets/{id}/decision` same decisionbody; Manager ownDept / HRall, never self, Submitted only -> Approved/Rejected. Rejection note3..300.

## Demo payroll

Manual salary inputs in VND, not statutory tax/insurance calculation or bank transfer. Gross=baseSalary+allowances+bonus; net=gross-deductions. All values0..1trillion with2decimals, deductions<=gross, note<=500. Employee name snapshotted when editing a draft.

- GET `/payroll/periods` HR/Admin only => `[{id,month,title,status,itemCount,totalGross,totalNet,createdAtUtc,publishedAtUtc,createdById,approvedById}]`.
- POST `/payroll/periods` HR/Admin `{month:'2026-11',title:'Lương tháng 11'}` => `{id}`. Unique month2000..2100.
- GET `/payroll/periods/{id}/items` HR/Admin => `[{id,periodId,employeeId,employee,baseSalary,allowances,bonus,deductions,gross,net,note}]`.
- PUT `/payroll/periods/{id}/items/{employeeId}` HR/Admin `{baseSalary,allowances,bonus,deductions,note}` upserts only Draft. DELETE same path Draftonly.
- POST `/payroll/periods/{id}/approve` HR/Admin Draft->Approved; requires at least one item. Same creator allowed in demo.
- POST `/payroll/periods/{id}/reopen` HR/Admin Approved->Draft only.
- POST `/payroll/periods/{id}/publish` Admin only Approved->Published (locked), persists publishedAtUtc and notifications.
- GET `/payslips` => PayrollItemDto + `{month,title,publishedAtUtc,currency:'VND',isDemo:true}`. Own Published only, HR/Admin all Published. Manager cannot see teammate salary.
- GET `/payslips/{id}` same scope; unauthorized/missing404. Draft unavailable to all via payslip endpoint.

## Offboarding

- GET `/offboarding` own/dept/all => `[{id,employeeId,employee,lastWorkingDate,reason,status,completedAtUtc,checklist:[{id,title,done}]}]`.
- POST `/offboarding` HR/Admin `{employeeId,lastWorkingDate:'yyyy-MM-dd',reason:'5..500chars'}` => `{id}`. Active employee, no self; HR cannot act on Admin. One case/employee, creates3standardchecks, statusInProgress.
- PUT `/offboarding/{id}/tasks/{taskId}` HR/Admin `{done:true}`; same ownership restrictions, InProgressonly.
- POST `/offboarding/{id}/complete` HR/Admin no body. All3checks done, lastWorkingDate<=today, no self/last-admin; HRcannotdeactivateAdmin. Sets Inactive, resets security stamps and disables accounts, releases assigned assets, removes active allocations, cancels Pending requests, caseCompleted. All one transaction; repeated409. Existing historic contracts/attendance/payroll retained. External GitHub/Google access is manual, not automatically revoked.

## ATS hiring

- GET `/candidates` existing fields + `hiredEmployeeId:number|null`.
- POST `/candidates/{id}/hire` HR/Admin `{departmentId,jobTitle,workMode:'Hybrid'|'Remote'|'Onsite',joinedAt?:'yyyy-MM-dd'}` => `{employeeId,message}`. Candidate Offer/Hired only, no previous CandidateHire; email must not match existing employee/account. Atomically creates employee +3onboarding tasks, marks Hired. Does not create login (Admin Accounts flow does).
- PUT existing `/candidates/{id}/stage` forbids stage changes after a hire link exists.

## Persistence/concurrency

OperationsSchema adds12tables (versionhistory included) with uniqueconstraints/FKs and no destructive mutation of old data. Operations writes serialize through a single operation lock row inside a Serializable transaction. Auth middleware translates database concurrency/duplicate errors to409; clients reload and retry. Suitable for small demo workload; scalable peremployee/period locking is future work.
