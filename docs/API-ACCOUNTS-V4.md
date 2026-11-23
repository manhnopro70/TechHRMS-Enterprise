# API tài khoản và phiên đăng nhập — V4

API cùng origin tại `/api`. Dùng cookie `techhrms_session`, không dùng token trong localStorage. Mọi phản hồi API đặt `Cache-Control: no-store`. Các vai trò hiện có: `Admin`, `HR`, `Manager`, `Employee`.

## CSRF

1. `GET /api/auth/csrf` cùng cookie jar → `{ "token": "..." }`. Lưu cookie `techhrms_csrf` mà máy chủ gửi.
2. Mọi `POST`, `PUT`, `DELETE` gửi `X-CSRF-TOKEN: <token>` và cookie jar. Bao gồm đăng nhập, đăng xuất, đổi mật khẩu.
3. Sau đăng nhập / đăng xuất / đổi mật khẩu, lấy token mới. Token trước đăng nhập gắn với người dùng ẩn danh, không tái sử dụng cho phiên đã đăng nhập.
4. Nếu máy chủ trả `403 {"code":"CSRF_INVALID","error":"..."}`, lấy token mới và thử lại **một lần**. Mã này có nghĩa thao tác đã bị chặn trước khi handler chạy. Không tự thử lại yêu cầu khi lỗi mạng không rõ kết quả.

## Phiên

`POST /auth/login`: `{ "email": "...", "password": "..." }`.

`GET /auth/me`: trả về Session sau khi đăng nhập:

```json
{"id":4,"name":"Nhân viên thử nghiệm","email":"employee@example.test","role":"Employee","jobTitle":"Developer","department":"Engineering","mustChangePassword":false}
```

`id` trong Session là **ID nhân viên**. `id` trong `/accounts` là **ID tài khoản**.

`POST /auth/logout`: không cần body → `{ "message": "Đã đăng xuất." }`.

Cookie tồn tại trong phiên trình duyệt, vé xác thực tối đa 8 giờ với gia hạn khi sử dụng. Khóa Data Protection lưu trong `DataProtectionKeys` cùng database. Khóa/mở tài khoản, thay vai trò, đặt lại/đổi mật khẩu, thay email/phòng ban/trạng thái nhân viên hoặc thu hồi phiên làm đổi `SecurityStamp`; cookie mang stamp cũ bị từ chối ngay ở yêu cầu kế tiếp.

Nâng cấp lần đầu từ V3 yêu cầu đăng nhập lại vì cookie V3 không có SecurityStamp. Các lần khởi động lại sau đó dùng cùng database giữ được key/stamp. Không có mật khẩu nào bị đổi trong quá trình nâng cấp.

## Đổi mật khẩu bắt buộc

Tài khoản mới cấp hoặc được Admin đặt lại mật khẩu có `mustChangePassword=true`. Backend chỉ cho các URL sau hoạt động với phiên này:

- `/auth/me`
- `/auth/profile`
- `/auth/csrf`
- `/auth/change-password`
- `/auth/logout`

Các chức năng khác trả `403 {"code":"PASSWORD_CHANGE_REQUIRED","error":"..."}`. Đây là kiểm tra phía máy chủ, không chỉ ẩn menu.

`POST /auth/change-password`:

```json
{"currentPassword":"mật khẩu hiện tại","newPassword":"mật khẩu mới dài 12–128 ký tự"}
```

Trả Session mới, `mustChangePassword=false`, cấp lại cookie và thu hồi cookie cũ trên mọi thiết bị. Hai mật khẩu phải khác nhau. Mật khẩu không đúng → 400. Giới hạn 10 lần/phút/tài khoản.

## Hồ sơ cá nhân

`GET /auth/profile`:

```json
{"id":4,"fullName":"Nhân viên thử nghiệm","email":"employee@example.test","role":"Employee","jobTitle":"Developer","departmentId":1,"department":"Engineering","workMode":"Hybrid","status":"Active","isBench":false,"joinedAt":"2026-11-23","mustChangePassword":false}
```

`PUT /auth/profile {"fullName":"Họ tên mới"}` → `{ "message": "..." }`. Chỉ cập nhật tên của chính người đang đăng nhập. Email, phòng ban, chức danh và trạng thái do HR/Admin cập nhật qua `/employees`.

## Quản trị tài khoản — chỉ Admin

`GET /accounts` trả mảng:

```json
[{"id":7,"employeeId":9,"employeeName":"Nhân viên thử nghiệm","email":"new@example.test","role":"Employee","isActive":true,"mustChangePassword":true,"createdAtUtc":"2026-11-23T00:00:00Z","lastLoginAtUtc":null}]
```

Không API nào trả `passwordHash` hoặc `securityStamp`.

| Endpoint | Body | Kết quả |
| --- | --- | --- |
| `POST /accounts` | `{employeeId, role, password}` | `{id,message}`; dùng email trong hồ sơ nhân viên, mật khẩu tạm, yêu cầu đổi lần đầu |
| `PUT /accounts/{accountId}` | `{role,isActive}` | `{message}`; đổi quyền/khóa/mở khóa và thu hồi phiên cũ |
| `POST /accounts/{accountId}/reset-password` | `{password}` | `{message}`; đặt mật khẩu tạm, buộc đổi và thu hồi phiên cũ |
| `POST /accounts/{accountId}/revoke-sessions` | Không cần body | `{message}`; thu hồi toàn bộ phiên, giữ nguyên mật khẩu |

Quy tắc:

- Chỉ một tài khoản cho mỗi nhân viên; email tài khoản duy nhất. Cấp trùng → 409.
- Không cấp/mở tài khoản cho nhân viên `Inactive`.
- Không tự khóa/hạ quyền tài khoản đang sử dụng.
- Không vô hiệu hóa quản trị viên hoạt động cuối cùng. Kiểm tra trong transaction, phối hợp với offboarding.
- Không reset mật khẩu của chính mình bằng trang Admin; dùng đổi mật khẩu cá nhân.
- Thu hồi phiên của chính mình cũng đăng xuất trình duyệt hiện tại.
- Mọi thay đổi tài khoản được ghi audit; không ghi mật khẩu vào audit.

## Tạo hồ sơ và tài khoản cùng lúc

`POST /employees` (HR/Admin được tạo hồ sơ; chỉ Admin được cấp tài khoản):

```json
{
  "fullName":"Nhân viên thử nghiệm",
  "email":"new@example.test",
  "jobTitle":"Developer",
  "departmentId":1,
  "workMode":"Hybrid",
  "status":"Active",
  "isBench":false,
  "account":{"role":"Employee","password":"Mật khẩu tạm từ 12 ký tự"}
}
```

Có thể bỏ `account` để chỉ tạo hồ sơ. Tạo employee + account + audit trong cùng transaction; trả `{id,accountCreated}`. HR gửi `account` → 403. Sai dữ liệu hoặc mật khẩu → 400 và không tạo dở hồ sơ.

`PUT /employees/{employeeId}` giữ body V3 và **bỏ `account`**. Gửi account khi chỉnh sửa → 400; dùng trang tài khoản để quản lý đăng nhập. Email tài khoản cập nhật theo email hồ sơ. Chuyển nhân viên sang `Inactive` sẽ khóa tài khoản; kích hoạt lại hồ sơ không tự mở lại tài khoản. HR không được sửa hồ sơ liên kết với tài khoản Admin. Không được tự vô hiệu hóa hồ sơ đang dùng.

`PUT /departments/{id}`: `{name,code}`, HR/Admin.

## Lỗi và giới hạn

- `400 {error}`: dữ liệu không hợp lệ.
- `401 {error,code?:"AUTH_REQUIRED"}`: chưa đăng nhập, cookie hết hạn/thu hồi, tài khoản khóa/nhân viên đã nghỉ. Chỉ lỗi login sai mật khẩu không chuyển hướng lặp.
- `403 {error,code?}`: không đủ quyền, CSRF không hợp lệ hoặc phải đổi mật khẩu.
- `404 {error}`: không tìm thấy tài nguyên; API không tồn tại không trả HTML của SPA.
- `409 {error,code?:"DATA_CONFLICT"}`: trùng dữ liệu hoặc xung đột đồng thời. Tải lại trước khi thử lại.
- `429 {error}`: quá nhiều lần thử; `Retry-After: 60`. Đăng nhập hiện giới hạn 60 lần/phút/IP để tránh một proxy dùng chung chặn cả nhóm; cần bổ sung chính sách chống tấn công nhiều máy/SSO khi vận hành lớn.
- `500 {error,code:"SERVER_ERROR"}`: lỗi máy chủ; chi tiết chỉ trong log máy chủ, không trả stacktrace cho trình duyệt.

## Database và triển khai

Thứ tự khởi động: `EnsureCreated` (chỉ tạo khi DB chưa có bảng) → `AccountSchema` → `Seed` → `EnterpriseSchema` → `OperationsSchema`.

`AccountSchema` thêm các cột vào `Accounts` nếu thiếu, tạo unique index `UX_Accounts_EmployeeId`, bảng `DataProtectionKeys` và nhật ký `AccountSchemaVersions`. Không xóa bảng, không đổi PasswordHash hay ghi đè tài khoản đã tồn tại. Nếu phát hiện nhiều tài khoản trên một nhân viên từ dữ liệu cũ, dừng nâng cấp với thông báo để xử lý thủ công, không tự xóa dữ liệu.

Production bắt buộc PostgreSQL. Trên Render, biến môi trường `RENDER=true` cho phép nhận `X-Forwarded-Proto` từ proxy Render, để cookie Secure/antiforgery nhận đúng HTTPS. Host khác phải cấu hình `TRUSTED_PROXY_IPS`; `TRUST_PROXY_HEADERS=true` chỉ dùng khi toàn bộ lưu lượng đi qua proxy tin cậy.

Đây vẫn là mô hình 4 vai trò của bản demo; không tuyên bố đã có SSO/MFA, gửi email đặt lại mật khẩu, phân quyền đa pháp nhân hoặc quản trị bí mật cấp doanh nghiệp.
