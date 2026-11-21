# TechHRMS — Full-stack starter v1.0

> Bản khởi đầu **gồm mã nguồn thật** cho đề tài hệ thống quản lý nhân sự công nghệ. **Không phải sản phẩm production-ready**. Frontend đã được kiểm tra cú pháp tĩnh, nhưng Backend cần được biên dịch và kiểm thử trên máy có .NET SDK. Được tạo để **GHÉP** vào dự án Vite React + TypeScript mà bạn đã có tại `C:\Users\Admin\Desktop\TechHRMS`.

## A. Bắt đầu: bạn đã hoàn thành đến đâu?

Bạn đã có:
- `frontend/` từ Vite (React + TypeScript).
- `frontend/node_modules/` sau `npm install`.
- Node.js v24.x, npm, .NET SDK 10 và Git.

Bản nâng cấp này thêm:
- `frontend/src/App.tsx`, `App.css`, `index.css`, `api.ts`, `types.ts`, `main.tsx`.
- `frontend/vite.config.ts`, `frontend/index.html`.
- `backend/` C# ASP.NET Core API + Entity Framework Core.
- Tài liệu, Dockerfile, script Windows.

**Không được xóa** `frontend/package.json`, `frontend/package-lock.json`, `frontend/node_modules/` của bạn. Chúng đã được tạo bởi Vite và **không nằm trong bản nâng cấp này**.

## B. BƯỚC 10 — Sao chép mã nguồn

1. Tại cửa sổ CMD đang chạy Vite, nhấn `Ctrl + C`, xác nhận `Y` nếu có hỏi. Có thể mở lại sau.
2. Mở file `TechHRMS-Fullstack-Starter.zip` và chọn **Extract All** (Giải nén tất cả).
3. Mở thư mục giải nén **TechHRMS-Fullstack-Starter** và chọn **tất cả các mục bên trong thư mục** (backend, frontend, docs, Dockerfile, README...).
4. Copy các mục đó vào **đúng** `C:\Users\Admin\Desktop\TechHRMS`.
5. Khi Windows hỏi về file trùng tên trong `frontend`, chọn **Replace the files in the destination**. Đừng kéo nguyên thư mục `TechHRMS-Fullstack-Starter` vào `TechHRMS` — phải hợp nhất nội dung.
6. Trong VS Code bấm vào các file mới dưới `frontend/src`, kiểm tra có `api.ts` và `types.ts`. Bên cạnh `frontend` phải có thêm thư mục `backend`.

Cấu trúc đúng:

```text
TechHRMS/
├── frontend/
│   ├── package.json        (file Vite cũ của bạn, giữ lại)
│   ├── package-lock.json   (file Vite cũ của bạn, giữ lại)
│   ├── node_modules/       (thư viện đã cài, giữ lại)
│   ├── index.html          (được thay)
│   ├── vite.config.ts      (được thay: có proxy /api)
│   └── src/
│       ├── App.tsx         (được thay)
│       ├── App.css         (được thay)
│       ├── index.css       (được thay)
│       ├── main.tsx        (được thay)
│       ├── api.ts          (mới)
│       └── types.ts        (mới)
├── backend/
│   ├── TechHRMS.Api.csproj
│   ├── Program.cs
│   ├── Models.cs
│   ├── Security.cs
│   ├── Seed.cs
│   ├── Endpoints.Core.cs
│   ├── Endpoints.Work.cs
│   ├── Endpoints.Extended.cs
│   └── appsettings.json
├── docs/
├── CHAY-BACKEND.cmd
├── CHAY-FRONTEND.cmd
├── Dockerfile
└── .gitignore
```

## C. BƯỚC 11 — Chạy Backend (C# API + SQLite)

Mở **CMD mới** (khác cửa sổ chạy Vite) và nhập lần lượt:

```cmd
cd /d "%USERPROFILE%\Desktop\TechHRMS\backend"
dotnet restore
dotnet run --urls http://localhost:5200
```

- `dotnet restore` tải gói NuGet **cần Internet** ở lần đầu.
- `backend/Properties/launchSettings.json` tự đặt môi trường **Development** khi dùng `dotnet run`. Khi chạy bằng cách khác (không dùng launch profile), phải chủ động đặt `ASPNETCORE_ENVIRONMENT=Development` cho local.
- `dotnet run` tạo database **SQLite demo** trong `backend/techhrms.db` lần đầu, tạo nhân viên giả lập và tài khoản demo.
- Khi thấy `Now listening on: http://localhost:5200`, giữ cửa sổ này mở.
- Mở Chrome vào `http://localhost:5200/api/health`. Nếu hiện JSON có `"status":"ok"`, API đã chạy.
- Từ lần thứ hai có thể bấm `CHAY-BACKEND.cmd` để chạy nhanh.

Nếu báo **file in use**, **restore NuGet lỗi** hay **build failed**, chụp ảnh CMD để kiểm tra; đừng xóa project.

## D. BƯỚC 12 — Chạy Frontend (React)

Mở **CMD thứ hai**:

```cmd
cd /d "%USERPROFILE%\Desktop\TechHRMS\frontend"
npm run dev
```

Mở `http://localhost:5173/`. Vite nhận `/api/*` và chuyển tiếp tới Backend ở `localhost:5200`. Nhớ giữ **hai cửa sổ CMD** đang chạy.

### Tài khoản **chỉ tồn tại trên máy local / môi trường Development**

Mật khẩu chung demo: `LocalDemo@2026!`

| Vai trò | Email |
|---|---|
| Admin | `admin@techhrms.local` |
| HR | `hr@techhrms.local` |
| Manager | `manager@techhrms.local` |
| Employee | `employee@techhrms.local` |

Không sử dụng mật khẩu demo này trên bản công khai.

### Kiểm thử tương tác

1. Đăng nhập `admin@techhrms.local`: trang Dashboard hiển thị nhân viên, phòng ban, yêu cầu, phân bổ.
2. Mở **Nhân sự** > **Thêm nhân viên** > lưu. Đổi màn hình, vào lại để kiểm tra dữ liệu vẫn có.
3. Đăng nhập `employee@techhrms.local` > **Đơn từ & phê duyệt** > tạo đơn WFH.
4. Đăng nhập `manager@techhrms.local` > **Đơn từ & phê duyệt** > duyệt đơn nhân viên ở cùng phòng.
5. Đăng nhập lại Employee để kiểm tra trạng thái đơn đã đổi.
6. Chỉnh skill, phân bổ dự án, cấp phát thiết bị, chuyển vòng ứng viên; làm mới trang Chrome để kiểm tra dữ liệu đã lưu vào SQLite.

## E. Đưa website lên Internet bằng **một link** (sau khi chạy local thành công)

Khi bạn muốn public: GitHub repo → Render Web Service (Docker) → Render PostgreSQL. Ở bản deploy, ASP.NET Core phục vụ **cả API và file React đã build**, nên chỉ cần **một URL** như `https://techhrms-xxx.onrender.com`.

1. Tạo GitHub repo (tốt nhất **Private** khi còn thử nghiệm). Từ thư mục gốc `TechHRMS`: `git init`, `git add .`, `git commit -m "TechHRMS fullstack starter"`. Tạo remote và push bằng hướng dẫn của GitHub.
2. Tạo **PostgreSQL database mới, trống** trên Render. Không dùng dữ liệu thật. Chú ý: gói Free Postgres Render **hết hạn sau 30 ngày**, không backup tự động.
3. Trên Render tạo **Web Service**, kết nối GitHub repo, chọn **Docker**. `Dockerfile` nằm tại gốc repo. Render sẽ build React và ASP.NET Core trong cùng một Docker image.
4. Ở Web Service, cấu hình biến môi trường (Environment):
   - `ASPNETCORE_ENVIRONMENT` = `Production`.
   - `ConnectionStrings__Postgres` = chuỗi kết nối Npgsql dạng **`Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require`**. **Không** đưa giá trị thật vào GitHub. Lấy giá trị host/database/user/password ở Render PostgreSQL, ưu tiên kết nối nội bộ cùng vùng; cấu hình SSL phù hợp với loại kết nối.
   - `SEED_ADMIN_EMAIL` = email bạn dùng để đăng nhập Admin lần đầu.
   - `SEED_ADMIN_PASSWORD` = mật khẩu **riêng** ít nhất 12 ký tự, khác mật khẩu local.
   - `SEED_SAMPLE_DATA` = `true` nếu muốn có một số nhân viên **giả lập** để demo.
   - `SEED_DEMO_PASSWORD` = mật khẩu riêng >=12 ký tự nếu muốn tạo 3 tài khoản thử nghiệm HR/Manager/Employee ngoài Internet (`hr@techhrms.local`, `manager@techhrms.local`, `employee@techhrms.local`). Không đặt biến này nếu chỉ muốn Admin được phép vào. Chỉ sử dụng dữ liệu giả khi chia sẻ tài khoản demo.
5. Deploy, kiểm tra `https://<your-app>.onrender.com/api/health`. Nếu trả về JSON `status: ok`, mở URL gốc để thấy đăng nhập React.
6. Đăng nhập bằng `SEED_ADMIN_EMAIL`/`SEED_ADMIN_PASSWORD`; chỉ chia sẻ thông tin đăng nhập cho người được phép. Link riêng không tự cấp tài khoản.

**Không dùng GitHub Pages để chạy trực tiếp C# API.** Không triển khai bản public bằng SQLite trên container miễn phí vì hệ thống file thường không bền vững.

### LƯU Ý CẤU TRÚC DATABASE

Bản v1.0 dùng `EnsureCreated()` chỉ để thiết lập CSDL trên một database **mới và trống**. Nếu Postgres đã có bảng từ dự án khác, `EnsureCreated()` không tạo bảng TechHRMS. Không dùng bản demo này để lưu dữ liệu thật; cần chuẩn bị **EF Core Migrations**, backup/restore và quy trình nâng cấp schema trước khi vận hành nghiêm túc.

## F. Phân hệ thực sự có mã chạy trong v1.0

- Đăng nhập, đăng xuất, Cookie HttpOnly, mật khẩu băm PBKDF2, giới hạn số lần login.
- Phân quyền Admin, HR, Manager, Employee tại Backend, giới hạn phạm vi xem dữ liệu.
- Dashboard đếm dữ liệu thực từ Database, biểu đồ phân bổ nhân sự.
- Nhân viên: xem, tìm kiếm, thêm, sửa; CSV export.
- Phòng ban: danh sách, thêm.
- Workflows nghỉ phép, WFH, OT: tạo đơn, duyệt/từ chối, kiểm tra không tự duyệt.
- Skill Matrix: xem, lọc, thêm, xóa.
- Dự án và phân bổ công suất, kiểm tra tổng ≤100%.
- Quản lý tài sản IT: thêm, cấp, thu hồi.
- Tuyển dụng ATS: thêm ứng viên, chuyển vòng tuyển dụng qua dropdown Kanban.
- OKR: tạo mục tiêu, cập nhật tiến độ.
- Audit Log thay đổi chính, chỉ Admin xem.
- Giao diện responsive desktop/mobile, backend phục vụ React trên cùng một tên miền khi deploy Docker.

## G. Những phần **chưa có** (không tuyên bố đã hoàn chỉnh)

- Tự đăng ký tài khoản, mời nhân viên, quên/đổi mật khẩu, email xác thực, SSO Google/Microsoft.
- Payroll thực tế (lương, thuế, BHXH, giờ OT), PDF payslip, hợp đồng, dữ liệu CCCD/ngân hàng.
- Quy tắc ngày phép tích lũy, ngày lễ, ca làm, attendance IP/GPS, lịch làm việc và lịch nghỉ.
- Thông báo push thời gian thực qua SignalR và Slack/Teams.
- File storage bảo mật (CV, bằng cấp, hợp đồng), phân quyền tải file.
- Đa công ty, chi nhánh/pháp nhân, phân cấp nhiều tầng và ủy quyền duyệt.
- Migrations có version, automated tests, CI/CD, monitoring, disaster recovery, backup, rate-limit nâng cao, bảo mật chống XSS/CSRF nâng cao, kiểm tra tuân thủ pháp lý.

## H. Kiểm thử và nâng cấp

Xem `docs/KE-HOACH.md` và `docs/KIEM-THU.md`.

Tài liệu tham khảo:
- https://learn.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-10.0
- https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/
- https://render.com/docs/free
- https://learn.microsoft.com/en-us/ef/core/managing-schemas/ensure-created
