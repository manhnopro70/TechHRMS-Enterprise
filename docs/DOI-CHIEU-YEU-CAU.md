# TechHRMS V3 - doi chieu voi phan tich HRM cap doanh nghiep

> Canh bao: He thong nay la **demo full-stack dung du lieu gia**, chua dap ung yeu cau van hanh HR thuc te. Bang nay phan biet ro 3 muc **co API va Database**, **co mot phan**, **chua trien khai**.

| Nhom chuc nang | Tinh trang V3 | Ghi chu ve pham vi |
| --- | --- | --- |
| Accounts & roles | Co mot phan | Admin, HR, Manager, Employee, cookie auth; chua co Finance, IT Admin, C-level, SSO/MFA |
| Core HR - employee | Co mot phan | CRUD them/sua va tim kiem; chua co CC CD, ngan hang, BHXH, lich su hieu luc, tai lieu |
| Departments | Co API | Danh sach, them; chua co legal entity, branch, team hierarchy |
| Organization chart | Co API V3 | So luoc theo phong ban, chua phai org tree da phap nhan |
| Contracts | Co API V3 | Them, xem theo quyen, cap nhat trang thai; khong co van ban/chuky so/luong |
| Onboarding | Co API V3 | Checklist, vai tro phu trach, han, tick hoan thanh; chua co provision tai khoan tu dong |
| Offboarding | Chua | Workflow ban giao, thu hoi, chot cong/luong chua lam |
| Attendance | Co API V3 | Check-in/out co thoi diem tu server, lich su thang; KHONG GPS/IP verified, ca, phep, payroll |
| Leave/WFH/OT | Co mot phan | Tao don/duyet mot cap, chua co so phep ton, ca lam viec, overtime policy |
| Skill Matrix | Co API | Them/xoa, level, certificate text; chua co expiring-alert |
| ATS | Co mot phan | Ung vien va thay stage; chua phieu phong van, offer letter, calendar |
| Project allocation | Co API | Du an, % allocation, bench; chua timesheet, billable, forecast |
| IT Assets | Co API | Device asset tag, cap phat; chua license, offboarding provisioning |
| OKRs | Co mot phan | Muc tieu va tien do; chua evaluation cycle, 360, calibration, 9-box |
| Learning & Development | Co API V3 | Danh muc khoa hoc, dang ky, tu cap nhat trang thai; chua LMS, certificate verification |
| HR Analytics | Co API V3 | Headcount, work mode, pending, attendance count; KHONG bao cao chi phi hay turnover lich su |
| Payroll/C&B/Tax/Insurance | Chua | Can policy table co ngay hieu luc, phe duyet, cac quy dinh hien hanh, kiem thu nghiep vu |
| Internal engagement | Chua | Bang tin, eNPS, kudos, surveys, chatbot |
| Workflow engine | Chua | Duyẹt Leave/WFH/OT mot cap; chua configurable multi-step approval |
| Fine-grained ABAC | Co mot phan | Chi scope nhan vien/phong ban va 4 roles; chua phap nhan/chi nhanh |
| Audit trail | Co mot phan | Log thay doi chinh, chua full tamper-evident audit, retention |
| Notifications | Co UI tạm | Chuong thong bao don tu hien co, khong phai realtime/server push |
| Excel/PDF and import | Co mot phan | Co xuat CSV nhan vien an toan; chua import mass, PDF, report builder |
| Integrations | Chua | SSO, Microsoft 365, Google Workspace, Slack, Teams, Jira, MISA, ngan hang ... |
| Enterprise compliance | Chua | Chua privacy impact assessment, retention, access/deletion workflow, MFA, encryption key management |
| Production maturity | Chua | EnsureCreated + additive DDL demo; can migrations, backup/DR, secrets, CI, tests, pen test, monitoring |

## Cap do thuc thi

- **Demo co backend that**: endpoint bao ve bang cookie auth va server-side role access, EF Core/SQLite local, PostgreSQL khi cau hinh Cloud. Khong co du lieu nhan su that.
- **Do chinh xac**: du lieu thoi gian/tien do chi de minh hoa, khong phai bang cong/phieu luong co gia tri phap ly.
- **Can ngan rui ro**: khong upload CCCD, hop dong that, payslip, so tai khoan ngan hang hay du lieu luong len demo public.

## Ke hoach phat trien theo pha

1. **Pha A - demo tham du cuoc thi**: kiem tra 4 roles, 10 modules cu + 6 modules V3, test CRUD, phan quyen, offline and remote breakpoints.
2. **Pha B - nghiep vu HR chuyen sau**: legal entity/branch, employee status effective dating, leave balance, OT approval multi-level, onboarding/offboarding state machine, background jobs.
3. **Pha C - tai chinh/bao mat**: payroll luat co hieu luc thoi gian, 2-person approval, pay period lock, payslip access, audit, PII encryption and retention, MFA/SSO.
4. **Pha D - production**: migration history, role permission policy engine, automated tests, CI/CD, database backup/restore drills, security review, performance testing, alerts/monitoring.
