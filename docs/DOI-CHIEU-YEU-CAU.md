# TechHRMS V4 — đối chiếu yêu cầu và phạm vi thực hiện

Bộ mã V4 nối Frontend–API–Database cho các phần được ghi là **đã có**. “Một phần” có nghĩa luồng mô tả chạy được nhưng chưa đầy đủ toàn bộ phân tích HRM của người dùng. Không coi một menu hay dữ liệu minh họa là chức năng đã hoàn thành.

| Nhóm yêu cầu | V4 thực hiện | Giới hạn còn lại |
| --- | --- | --- |
| Tài khoản/đăng nhập | Admin cấp từ hồ sơ có sẵn hoặc lúc tạo hồ sơ; 4 vai trò; đổi/reset mật khẩu; bắt buộc đổi lần đầu; khóa/mở/thu hồi phiên | Chưa SSO, MFA, tự gửi mail, tự đăng ký công khai |
| Phân quyền | Backend kiểm tra vai trò và phạm vi self/phòng ban/toàn hệ thống; giới hạn lương riêng | Chưa Finance/ITAdmin/Executive độc lập, ABAC pháp nhân/chi nhánh |
| Core HR | Tạo/sửa/tìm hồ sơ, email, chức danh, phòng ban, trạng thái, work mode, bench; xem/sửa tên cá nhân | Chưa CCCD/ngân hàng/BHXH/người phụ thuộc, effective dating, tài liệu upload |
| Tổ chức | Thêm/sửa phòng ban, tổng hợp số người | Chưa nhiều pháp nhân, chi nhánh, team và cây báo cáo nhiều cấp |
| Hợp đồng | Danh mục hợp đồng, thời hạn, trạng thái, phạm vi xem | Chưa mẫu văn bản thực, phụ lục, ký số và nhắc hạn qua email |
| ATS | Ứng viên, Kanban kéo thả/select, đổi vòng; từ Offer/Hired tạo nhân viên và onboarding đúng một lần | Chưa parse/upload CV, requisition, đăng job ra ngoài, lịch/scorecard/offer letter |
| Onboarding | Checklist theo nhân viên, người phụ trách theo vai trò, hạn/trạng thái; ATS tạo checklist tự động | Chưa tự cấp GitHub/Jira/Google/Slack; Admin cấp tài khoản HRM trong màn tài khoản |
| Offboarding | Checklist, ngày cuối, hoàn tất có transaction; khóa HRM, đổi stamp, thu hồi tài sản/phân bổ, hủy đơn chờ, giữ lịch sử | Chưa khóa hệ thống bên ngoài, tính quyết toán cuối, exit interview hoặc tái tuyển dụng cùng hồ sơ offboarding |
| Chấm công | Check-in/out theo giờ server, lịch sử tháng, không mở phiên mới khi phiên cũ chưa đóng | Chưa GPS/IP verified, face/máy chấm công, ca linh hoạt, sửa công/giải trình, tổng hợp công chuẩn payroll |
| Phép/WFH/OT | Tạo, duyệt/từ chối, lý do từ chối, hủy đơn chờ; giữ phép pending; kiểm tra trùng; lưu giờ OT | Chưa lịch lễ/nửa ngày/phép chuyển năm/loại phép chuyên sâu, duyệt đa cấp và chính sách OT pháp định |
| Hạn mức phép | Theo nhân viên/năm, HR cấu hình; tính thứ Hai–thứ Sáu, đã dùng/đang chờ/khả dụng | Mặc định demo 12 ngày không phải tính quyền lợi theo hợp đồng/pháp luật |
| Dự án/nguồn lực | Tạo dự án, phân bổ phần trăm, giới hạn tổng 100%, thu hồi | Chưa allocation theo khoảng hiệu lực, dự báo staffing hoặc tích hợp Jira |
| Giờ công dự án | Nháp/sửa/xóa/gửi/duyệt/từ chối, billable flag, dự án được phân bổ, tối đa24 giờ/ngày | Chưa hóa đơn khách hàng, khóa kỳ timesheet hoặc định giá giờ công |
| Tài sản IT | Tạo tài sản, cấp/thu hồi, liên kết offboarding | Chưa software license, lịch sử bảo hành/bảo trì hoặc API cấp quyền |
| Kỹ năng | Skill matrix theo người, level, tên chứng chỉ, tìm nhân sự | Chưa ngày hết hạn/cảnh báo chứng chỉ hoặc chuẩn hóa khung năng lực |
| Hiệu suất | OKR, quý, người sở hữu, cập nhật tiến độ | Chưa chu kỳ tự/manager review,360°, calibration, 9-box, PIP |
| Đào tạo | Danh mục khóa học, đăng ký, trạng thái học | Chưa LMS/nội dung video/kiểm tra/chứng chỉ được xác minh |
| Lương demo | Tạo kỳ, nhập khoản thu/khấu trừ, tổng gross/net, duyệt, Admin phát hành, khóa, xem phiếu theo quyền, in/PDF qua trình duyệt | Chưa công/OT tự tính vào lương, thuế/BHXH theo luật, nhiều tiền tệ, chuyển khoản, hai người độc lập duyệt |
| Phúc lợi/chi phí | Chưa có | Expense, hoàn ứng, phúc lợi, ESOP, reviewlương, ngân sách |
| Thông báo | Lưu DB theo người, chưa đọc, đánh dấu, link nghiệp vụ; UI cập nhật mỗi 30 giây khi hiển thị | Chưa server push/SignalR, email/Slack/Teams/push mobile |
| Audit | Ghi thao tác chính, người thao tác/thời gian; không log mật khẩu | Chưa toàn bộ old/new snapshot, retention, chống sửa nhật ký bằng quản trị DB |
| Báo cáo | Headcount/hình thức làm việc/phòng ban/đơn/chấm công, CSV nhân viên, tổng lương nhập | Chưa turnover lịch sử, chi phí tuyển dụng, report builder, toàn bộ Excel/PDF/import |
| Gắn kết | Chưa có | Bảng tin/kudos/khảo sát/eNPS/chatbot |
| Đa ngôn ngữ/quốc tế | UI tiếng Việt, định dạng thời gian phù hợp demo; múi giờ Backend cấu hình | Chưa bộ dịchViệt/Anh, đa tiền tệ, visa/workpermit |
| Database | SQLite local, PostgreSQL triển khai; nâng cấp có phiên bản, thêm bảng/cột/index; giữ dữ liệu cũ | Chưa chuyển toàn bộ lịch sử sang EF migrations; bảng V3 cũ giữ thiếu FK như trước |
| Chất lượng triển khai | Docker full-stack, health, lỗi API, CSRF, thu hồi phiên, khóa lưu DB, kiểm thử HTTP/PG, CI build | Chưa kiểm toán bảo mật độc lập, kiểm tải lớn, backup/DR tự động hoặc chứng nhận tuân thủ |
| Windows Desktop | Mã WinForms/WebView2 đầy đủ, URL cấu hình, LocalAppData, thử lại, mở trình duyệt, script publish | Cần Internet/WebView2 Runtime; chưa kiểm thử cài MSI trên Windows trong môi trường này; không offline |

## Phân biệt demo hoạt động và hệ thống doanh nghiệp

Bản V4 có nhiều người đăng nhập bằng quyền khác nhau, lưu dữ liệu lâu dài trên database được cấu hình, xử lý các quy trình và có kiểm tra phía Backend. Điều đó đáp ứng một demo tương tác thực tế để trình bày sản phẩm.

Phần phân tích HRM ban đầu còn bao gồm các dự án lớn độc lập như payroll pháp định, danh tính SSO/MFA, tích hợp bên thứ ba và tổ chức đa pháp nhân. Những phần này không được trình bày như đã có trong V4. Nếu phát triển tiếp, cần bổ sung thiết kế và kiểm thử riêng cho từng nhóm, đồng thời nâng cấp nền tảng bảo mật/vận hành.
