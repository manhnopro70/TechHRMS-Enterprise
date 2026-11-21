# Roadmap triển khai TechHRMS

## 1. Các cổng truy cập

**ESS / Employee:** thông tin bản thân, kỹ năng, đơn nghỉ phép/WFH/OT, tài sản đang giữ, dự án tham gia, OKR cá nhân.

**MSS / Manager:** danh sách thành viên trong phòng ban, duyệt đơn của cấp dưới, tiến độ và công suất nguồn lực, kỹ năng của team, OKR team.

**HR & Admin:** danh sách nhân viên và phòng ban toàn công ty, tuyển dụng ATS, tài sản IT, yêu cầu nhân viên, nguồn lực, audit. Admin có quyền audit.

## 2. Các giai đoạn phát triển

| Giai đoạn | Phạm vi | Thành công khi |
|---|---|---|
| A - nền tảng demo | Frontend, Backend, SQLite, session, 3 portal, 8 phân hệ | Người dùng đăng nhập 4 vai trò, thêm và thấy dữ liệu ở trình duyệt khác sau khi refresh |
| B - public demo | Docker, PostgreSQL mới, Render, HTTPS, link truy cập | URL công khai hoạt động, restart không mất dữ liệu |
| C - workflow hoàn chỉnh | Danh sách nhân viên, quản lý tài khoản, quản lý phép, IP/GPS có cân nhắc riêng tư, lịch làm | Phê duyệt theo người chịu trách nhiệm, có kiểm tra giới hạn nghiệp vụ |
| D - chuẩn doanh nghiệp | SSO, MFA, audit nâng cao, encryption PII, backup, EF migrations, test, email, SignalR | Vượt checklist bảo mật; không có truy cập chéo nhân sự |
| E - nghiệp vụ chuyên sâu | Payroll, tax, insurance, ATS assessment, OKR 360°, asset account integrations | Kiểm thử nghiệp vụ với chuyên gia HR & pháp chế, đối soát kết quả |

## 3. Điều quan trọng khi bảo vệ đề tài

Phân biệt **prototype/demo** và sản phẩm vận hành doanh nghiệp. Bản v1.0 minh họa kiến trúc full-stack thực: React gọi ASP.NET Core API, EF Core lưu SQLite/PostgreSQL, backend kiểm tra người dùng và vai trò. Dashboard, bảng nhân viên và workflow là dữ liệu thật được lưu trong CSDL; không chỉ là UI tĩnh.

Không tuyên bố chức năng thời gian thực, SSO, payroll tự động hay ràng buộc pháp luật nếu chưa triển khai thật.

## 4. Nâng cấp mô hình dữ liệu về sau

- Organization, LegalEntity, OfficeLocation, Team, Position, Grade, ReportingLine.
- EmploymentContract, ContractVersion, EmployeeDocument, ConsentRecord.
- Shift, AttendanceLog, LeavePolicy, LeaveBalance, HolidayCalendar, ApprovalStep, Delegation.
- PayrollPeriod, PayrollComponent, PayrollRun, Payslip, TaxRuleVersion.
- Certificate, CertificateExpiry, SkillValidation, CompetencyFramework.
- ProjectAssignmentVersion, BillableStatus, BenchHistory, CostCenter.
- Notification, NotificationDelivery, AuditEvent (append-only), SecurityEvent.
- AccountInvitation, MFARecovery, SSOIdentity, RolePermission, ResourcePolicy.

## 5. Nguyên tắc vận hành

- Ràng buộc quyền truy cập ở Backend và tới từng đối tượng (employee id / team / organization).
- Không lưu mật khẩu/connection string trong GitHub; dùng secret của host.
- Không lưu CCCD hay lương thật trong demo công khai.
- Tạo migration, sao lưu, test, audit trước khi có người dùng thực.
