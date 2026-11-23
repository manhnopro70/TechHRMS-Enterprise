# Kết quả xác minh TechHRMS V4

Ngày thực hiện: 23/11/2026. Mã nền: repository TechHRMS-Enterprise, commit `2ad577a`. Mọi phép thử chạy trên dữ liệu tạo riêng; không truy cập hoặc sửa database Render của người dùng.

## Kết quả thực tế

| Hạng mục | Kết quả | Phạm vi |
| --- | --- | --- |
| Frontend production build | PASS | `npm ci`, TypeScript + Vite 8.3.8; JS 357.72kB, CSS 90.40kB trước gzip |
| Backend Release build | PASS | .NET SDK 10.0.404,0 lỗi; còn5 cảnh báo EF1002 trong DDL V3 dùng tên/kiểu nội bộ, không dùng đầu vào người dùng |
| HTTP/API với SQLite mới | PASS | 179 kiểm tra trạng thái/phản hồi/quy tắc thực tế |
| HTTP/API với schema SQLite V3 | PASS | 179 kiểm tra tương tự +2 kiểm tra giữ hash mật khẩu và hồ sơ cũ |
| Tổng bộ HTTP | PASS | 360 kiểm tra; gọi API thật qua cookie/CSRF, không mock database |
| PostgreSQL thật: V3 → V4 | PASS | Giữ toàn bộ8 hàng nhân viên và4 ID/email/role/hash tài khoản gốc; query và ghi nghiệp vụ mới |
| PostgreSQL mới và restart | PASS | Tạo mới, ghi dữ liệu; upgrade idempotent; cookie còn hiệu lực sau restart; key lưuDB |
| App Windows build/publish | PASS | Cross-build và self-contained publish `win-x64` tạo `TechHRMS.Desktop.exe`;0 lỗi; không chạy GUI Windows ở đây |
| Phục vụ React cùng API | PASS | HTTP200 cho HTML production, có root và JSbundle từ staticwebroot |
| Browser UI/mobile tự động | CHƯA CHẠY ĐƯỢC | Chromium bị môi trường chặn Unixsocket ngay lúc khởi động;0 assertion trình duyệt, không có screenshot |
| Cài MSI trên Windows | CHƯA KIỂM THỬ | Cần thực hiện trên Windows/Advanced Installer với payload mới |
| Docker image/Render production | CHƯA TRIỂN KHAI | Đã giữ Dockerfile, kiểm tra build hai thành phần; môi trường không có Dockerdaemon |
| GitHub CI | ĐÃ CẤU HÌNH | Workflow YAML đã kiểm tra; chưa chạy GitHubActions vì chưa push |

360 kiểm tra không có nghĩa360 kịch bản nghiệp vụ độc lập hoặc kiểm toán an toàn toàn diện. Các nhóm dưới đây mô tả nội dung đã được xác minh.

## Các ràng buộc đã kiểm tra

1. Đăng nhập cũ vẫn dùng được; mật khẩu không đổi khi upgrade schema.
2. Không cookie →401; thiếu CSRF khi đăng nhập →403; login sai mật khẩu →401.
3. DTO tài khoản không trả PasswordHash/SecurityStamp; không tự hạ quyền/khóa Admin đang dùng.
4. Tạo hồ sơ+kèm tài khoản thực sự hoạt động và yêu cầu đổi mật khẩu lần đầu; API nghiệp vụ bị chặn trước khi đổi.
5. Employee/Manager/HR không quản trị tài khoản; HR không tạo tài khoản Admin qua form nhân viên; yêu cầu thất bại không tạo hồ sơ dở.
6. Nhân viên chỉ thấy hồ sơ của mình; quản lý không thấy nhân sự ngoài phòng ban.
7. Cấp tài khoản cho hồ sơ có sẵn; chặn tài khoản trùng/mật khẩu quá ngắn.
8. Khóa rồi mở lại vẫn vô hiệu cookie cũ; reset mật khẩu thu hồi phiên và bật yêu cầu đổi mật khẩu.
9. Khởi động lại Backend giữ cookie hợp lệ nhờ key/stamp lưu database.
10. Đơn phép pending giữ hạn mức; duyệt đổi sang phép đã dùng; hủy giải phóng phép.
11. Kiểm tra trùng Leave/WFH, hạn mức không được thấp hơn phép dùng/đang chờ, lý do từ chối, phạm vi quản lý, không tự duyệt.
12. Hai yêu cầu phê duyệt đồng thời: đúng một thành công, yêu cầu còn lại409; không trừ phép hai lần.
13. Thông báo lưu thực; không đánh dấu thông báo của người khác; mark-all-read.
14. Timesheet cần phân bổ; kiểm tra trùng dự án/ngày, giới hạn tổng24h, trạng thái sau gửi/duyệt, phạm vi xem.
15. Check-in/out thực, chặn ghi trùng; skill, OKR, các endpoint V3 vẫn dùng được.
16. Payroll gross/net đúng; nhân viên không xem draft; HR không phát hành; Admin phát hành; kỳ đã phát hành bất biến; người khác và Manager không xem phiếu lương đồng đội.
17. Hire chỉ tạo nhân viên/checklist một lần; lần gọi lặp bị chặn.
18. Offboarding không hoàn tất trước checklist; hoàn tất khóa phiên, thu hồi thiết bị/phân bổ; phiếu lương lịch sử còn.
19. Audit có dữ liệu và không chứa mật khẩu thử nghiệm.

## Tự chạy lại

### Bộ HTTP mặc định — cần .NET10 và Python3

```powershell
dotnet build backend/TechHRMS.Api.csproj -c Release
python tests/run_integration.py --no-build
```

Runner tự dùng database tạm, khởi động API ở cổng trống, dừng API khi xong. Báo cáo nằm trong `test-results/integration-results.json`. Có thể chạy `--only fresh` hoặc `--only legacy`.

### PostgreSQL

`tests/postgres-smoke.mjs` dùng package kiểm thử `embedded-postgres` được cài ở thư mục riêng, cùng checkout nguồn V3 làm mốc. Xem phần đầu script về `POSTGRES_PACKAGE`, `BASELINE_PROJECT`, `DOTNET_PATH`. Script chỉ tạo database localhost, không lấy chuỗi kết nối Render. Kết quả đã chạy được lưu trong `docs/verification/postgres-results.json`.

### Browser UI — cần môi trường chạy Chromium được

```powershell
python -m pip install playwright
python -m playwright install chromium
python tests/ui-smoke.py --dotnet dotnet
```

Đây là script đã chuẩn bị, **chưa được xác nhận pass** do giới hạn môi trường nêu trên. Cần chạy và kiểm tra tương tác/màn hình desktop390px/mobile trước khi coi phần giao diện đã hoàn tất kiểm thử.

## Kiểm tra bằng tay trên Windows trước khi gửi demo

- Hai cửa sổ khác phiên: Admin/Manager và Employee; tạo tài khoản → đổi mật khẩu → gửi/duyệt đơn → xem thông báo.
- Kiểm tra các form tạo/sửa, điều hướng Back/Refresh, màn hình hẹp và hộp lỗi.
- In phiếu lương và lưu PDF; kiểm tra tên, kỳ, số tiền và phạm vi người xem.
- Sau khi cập nhật Render, mở website ở cửa sổ ẩn danh; mở App hiện tại cùng URL; kiểm tra login và dữ liệu dùng chung.
- Nếu đóng gói AppV4 mới, cài MSI và chạy bằng tài khoản Windows thường; kiểm tra tải lại khi mất Internet.

## Tài liệu kỹ thuật chính thức đã đối chiếu

- Microsoft: [Cookie authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0).
- Microsoft: [Antiforgery](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0).
- Microsoft: [DataProtection key storage](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0).
- Microsoft: [WebView2 userDataFolder](https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environment.createasync).
- Render: [Deploys](https://render.com/docs/deploys), [Docker](https://render.com/docs/docker), [Environment variables](https://render.com/docs/environment-variables).
