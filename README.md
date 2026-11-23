# TechHRMS V4 — Web + Windows Desktop

Bộ mã đầy đủ nâng cấp từ repository `manhnopro70/TechHRMS-Enterprise`, commit nền `2ad577a551aceec568f79963da13f2ddf9247be2`.

**Bắt đầu với [HUONG-DAN.md](HUONG-DAN.md)** nếu bạn cần từng bước chạy trên Windows, cập nhật bản cũ và đưa lên Render. Gói này có cả Frontend, Backend, mã Desktop, cấu hình Docker và kiểm thử; không phải gói giao diện riêng.

## Đã có

- Tài khoản Admin / HR / Manager / Employee; cấp tài khoản từ hồ sơ, tạo hồ sơ kèm tài khoản, khóa/mở, đặt mật khẩu tạm, buộc đổi lần đầu, thu hồi phiên.
- Hồ sơ nhân viên/phòng ban, danh bạ theo quyền, hợp đồng, kỹ năng, OKR, tài sản, dự án và phân bổ nguồn lực.
- Chấm công vào/ra; đơn nghỉ phép/WFH/OT; phép đã dùng/đang chờ/khả dụng; hủy đơn chờ duyệt; phê duyệt theo phạm vi.
- Giờ công dự án: nháp, chỉnh sửa, gửi duyệt, duyệt/từ chối; kiểm tra phân bổ và tổng giờ theo ngày.
- Tuyển dụng Kanban; tiếp nhận ứng viên tạo hồ sơ và checklist onboarding; bàn giao nghỉ việc có thu hồi quyền HRM, tài sản và phân bổ.
- Lương demo: nhập lương/phụ cấp/thưởng/khấu trừ, duyệt kỳ, phát hành và khóa; phiếu lương theo quyền, in/lưu PDF qua trình duyệt.
- Khóa học/đăng ký học, báo cáo tổng hợp, thông báo lưu database, audit log.
- Giao diện tiếng Việt, navy/jade, responsive, điều hướng URL, tìm phân hệ, xử lý lỗi phiên hết hạn.
- App Windows WebView2 dùng cùng website/database; thư mục dữ liệu người dùng, thử lại khi lỗi kết nối và mở website ngoài trình duyệt.

## Chạy local

Cài Node.js 24 và .NET SDK 10. Từ thư mục project:

```powershell
cd frontend
npm ci
npm run dev
```

Mở terminal khác:

```powershell
cd backend
dotnet run --urls http://localhost:5200
```

Mở `http://localhost:5173`. Dữ liệu SQLite tạo tự động trong `backend/techhrms.db` ở môi trường Development. Database mới có tài khoản `admin@techhrms.local`, `hr@techhrms.local`, `manager@techhrms.local`, `employee@techhrms.local`, mật khẩu local `LocalDemo@2026!`. Database đã tồn tại giữ nguyên tài khoản và mật khẩu.

## Cấu trúc

| Thư mục/tệp | Công dụng |
| --- | --- |
| `frontend/` | React + TypeScript + Vite; package và lockfile đầy đủ |
| `backend/` | ASP.NET Core 10, EF Core, SQLite/PostgreSQL |
| `desktop/` | WinForms + WebView2, cấu hình URL website |
| `Dockerfile` | Build frontend + API, phục vụ chung một địa chỉ |
| `BUILD-DESKTOP.cmd` | Publish Windows x64 self-contained |
| `tests/` | Kiểm thử HTTP, nâng database cũ, PostgreSQL, giao diện |
| `docs/` | API, phạm vi, kết quả kiểm thử và triển khai |
| `.github/workflows/verify.yml` | Build + kiểm thử, không tự triển khai |

## Kiểm thử

```powershell
cd frontend
npm ci
npm run build
cd ..
dotnet build backend/TechHRMS.Api.csproj -c Release
python tests/run_integration.py --no-build
```

Kiểm thử HTTP tạo database tạm riêng, chạy API rồi dừng; không truy cập Render. Có kiểm tra database mới và schema V3 cũ, phân quyền, CSRF, cấp/khóa/reset tài khoản, phiên qua restart, phép, giờ công, lương, tuyển dụng và bàn giao. Chi tiết xác minh thực tế ở `docs/KIEM-THU-V4.md`.

## Triển khai

Render tiếp tục dùng Dockerfile tại gốc và PostgreSQL hiện có. Giữ `ConnectionStrings__Postgres`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` trong môi trường Render. Production không dùng mật khẩu local mặc định. Nâng cấp bổ sung schema, không xóa database; cookie V3 cần đăng nhập lại một lần.

Nếu App Windows hiện tại đang mở `https://techhrms-enterprise.onrender.com/`, cập nhật web ở cùng URL sẽ cập nhật các chức năng mà App hiển thị. Bản Desktop mới trong gói là tùy chọn; chạy `BUILD-DESKTOP.cmd` rồi đưa toàn bộ thư mục publish vào Advanced Installer nếu muốn tạo bộ cài mới.

## Phạm vi chính xác

Đây là bản demo full-stack có nghiệp vụ và dữ liệu lưu thực. Lương dùng khoản nhập thủ công, không phải bộ tính thuế/BHXH pháp định hoặc chuyển lương ngân hàng. Phép tính theo thứ Hai–thứ Sáu và hạn mức cấu hình, chưa xử lý lễ/nửa ngày/chuyển năm. Thông báo cập nhật định kỳ 30 giây khi mở trang. Chưa có SSO/MFA, đa pháp nhân, máy chấm công/GPS xác thực, đánh giá 360°, email tự động hoặc cấp/thu hồi GitHub/Jira. Xem bảng đầy đủ tại `docs/DOI-CHIEU-YEU-CAU.md`.
