# Hướng dẫn TechHRMS V4 — chạy, cập nhật và gửi bản demo

## 1. Bạn đang nhận được gì?

Một bộ mã đầy đủ gồm `frontend`, `backend`, `desktop`, `tests`, `docs`, `Dockerfile`, package/lockfile và các script Windows. Không cần ghép lại Starter, V2, V3 và V3.1.

Bản này được chỉnh từ mã đầy đủ trên GitHub của bạn. ZIP V3.1 bạn gửi chỉ có `App.tsx`, `refined.css` và hướng dẫn, vì vậy bộ mã đầy đủ đã được đối chiếu với commit `2ad577a` trên repository trước khi sửa.

Các file không có trong ZIP: `node_modules`, `bin`, `obj`, database của bạn, `.git`, mật khẩu Render và bộ cài MSI cũ. Đây là các dữ liệu/phần sinh ra trên máy của bạn, không phải phần mã nguồn cần thay.

## 2. Cách chạy thử dễ nhất: giải nén ra một thư mục riêng

Nên xem bản mới trên máy trước khi đưa lên website đang gửi cho người khác.

1. Tải ZIP V4 và chọn **Extract All… / Giải nén tất cả**.
2. Đặt thư mục đã giải nén ở Desktop, ví dụ `C:\Users\Admin\Desktop\TechHRMS-V4`.
3. Mở vào thư mục. Tại cấp này phải nhìn thấy đồng thời `frontend`, `backend` và `Dockerfile`. Nếu vẫn còn một thư mục `TechHRMS-V4` bên trong, mở tiếp vào đó; hãy dùng đúng cấp có ba mục trên.
4. Mở VS Code → **File → Open Folder…** → chọn thư mục đó.
5. Mở **Terminal → New Terminal**.

### Terminal 1: chạy Backend

```powershell
cd C:\Users\Admin\Desktop\TechHRMS-V4\backend
dotnet restore
dotnet run --urls http://localhost:5200
```

Chờ dòng `Now listening on: http://localhost:5200`. Giữ cửa sổ này chạy.

Nếu máy báo cổng 5200 đang được sử dụng, hãy dừng Backend cũ bằng `Ctrl+C` trong cửa sổ cũ, rồi chạy lại. Không cần chạy hai bản Backend cùng cổng.

### Terminal 2: cài và chạy Frontend

Bấm dấu **+** ở khu vực Terminal để mở terminal thứ hai:

```powershell
cd C:\Users\Admin\Desktop\TechHRMS-V4\frontend
npm ci
npm run dev
```

`npm ci` tự cài dependency theo lockfile; cần Internet ở lần cài này. Chờ địa chỉ mà Vite in ra, thông thường `http://localhost:5173`. Mở địa chỉ này bằng Chrome.

### Đăng nhập database local mới

| Vai trò | Email | Mật khẩu local |
| --- | --- | --- |
| Quản trị | `admin@techhrms.local` | `LocalDemo@2026!` |
| HR | `hr@techhrms.local` | `LocalDemo@2026!` |
| Quản lý | `manager@techhrms.local` | `LocalDemo@2026!` |
| Nhân viên | `employee@techhrms.local` | `LocalDemo@2026!` |

Các tài khoản này chỉ tự sinh khi chạy Development với database mới. Nếu dùng database cũ, hãy đăng nhập bằng mật khẩu cũ của database đó. Không lấy mật khẩu trong bảng này để suy đoán mật khẩu của website Render hiện tại.

## 3. Cập nhật project cũ và giữ dữ liệu local

Thực hiện khi bạn đã xem bản mới và muốn cập nhật thư mục đang liên kết GitHub.

1. Trong mọi terminal đang chạy Frontend/Backend, nhấn `Ctrl+C` để dừng.
2. Mở Desktop trong File Explorer, sao chép thư mục `TechHRMS` thành `TechHRMS_Backup_Truoc_V4`. Giữ bản sao này để có thể khôi phục mã và SQLite cũ.
3. Trong gói V4, sao chép các thư mục `frontend`, `backend`, `desktop`, `docs`, `tests`, `.github` vào **bên trong** `C:\Users\Admin\Desktop\TechHRMS`.
4. Chọn **Replace the files in the destination** khi Windows hỏi trùng file. Đây là ghép/thay file trong thư mục, không xóa toàn bộ thư mục project cũ trước.
5. Sao chép thêm các file gốc: `Dockerfile`, `.dockerignore`, `.gitignore`, `README.md`, `HUONG-DAN.md`, `CHAY-BACKEND.cmd`, `CHAY-FRONTEND.cmd`, `BUILD-DESKTOP.cmd`.
6. Giữ `backend/techhrms.db` cùng các tệp đi kèm nếu có, `.git`, cấu hình riêng và các biến môi trường của bạn. ZIP không chứa database để ghi đè các file này.
7. Mở VS Code tại thư mục **TechHRMS cũ vừa cập nhật**. Trong `frontend`, chạy `npm ci` rồi `npm run build`. Trong `backend`, chạy `dotnet build` rồi `dotnet run --urls http://localhost:5200`.
8. Mở terminal khác chạy `npm run dev` trong `frontend`; vào website local và đăng nhập bằng tài khoản cũ.

Nâng cấp schema diễn ra khi Backend khởi động, bổ sung cột tài khoản và các bảng nghiệp vụ mới. Không có thao tác xóa database hoặc đổi mật khẩu cũ. Cookie V3 chưa có mã phiên mới nên lần đầu nâng cấp cần đăng nhập lại.

Nếu nâng cấp báo một nhân viên có nhiều tài khoản, giữ nguyên database và xem thông báo lỗi. Bản nâng cấp chủ động dừng thay vì tự xóa tài khoản để lấy một bản.

## 4. Cấp tài khoản cho các email nhân viên đang có

Đây là phần giải quyết vấn đề bạn hỏi trước đó: **email trong hồ sơ nhân sự không tự có mật khẩu đăng nhập**.

1. Đăng nhập bằng **Admin**.
2. Ở menu bên trái, chọn **Tài khoản & phân quyền**.
3. Bấm **Cấp tài khoản**.
4. Chọn hồ sơ nhân viên chưa có tài khoản. Email đăng nhập được lấy từ hồ sơ này.
5. Chọn vai trò `Nhân viên`, `Quản lý`, `Nhân sự` hoặc `Quản trị viên` đúng nhu cầu. Với nhân viên bình thường, chọn **Nhân viên**.
6. Nhập mật khẩu tạm tối thiểu **12 ký tự**, tối đa 128 ký tự.
7. Bấm **Tạo tài khoản**.
8. Gửi riêng email và mật khẩu tạm cho người sử dụng. Website chưa tự gửi email này.
9. Người dùng mở website hoặc App, đăng nhập. Hệ thống yêu cầu đổi mật khẩu tạm trước khi vào các phân hệ.

**Tạo nhân viên mới kèm tài khoản:** Admin vào **Hồ sơ nhân viên → Thêm nhân viên**, điền hồ sơ, bật tùy chọn tạo tài khoản, chọn vai trò và mật khẩu tạm. Khi lưu thành công, hồ sơ và tài khoản cùng được tạo. HR có thể tạo hồ sơ nhưng không tự cấp quyền Admin hay tài khoản đăng nhập.

**Quên mật khẩu:** Admin vào **Tài khoản & phân quyền → Đặt lại mật khẩu** của người đó, đặt mật khẩu tạm mới. Các phiên cũ bị thu hồi. Người dùng phải đổi mật khẩu ở lần đăng nhập tiếp theo. Admin muốn đổi mật khẩu của chính mình dùng **Tài khoản của tôi**.

**Khóa tài khoản:** vào **Phân quyền**, bỏ **Cho phép đăng nhập**, lưu. Mở lại bằng cách đánh dấu lại. Các cookie cũ không tự có hiệu lực trở lại sau khi mở khóa.

## 5. Chọn vai trò và phạm vi dữ liệu

| Vai trò | Có thể dùng |
| --- | --- |
| Admin | Quản trị tài khoản, nhân sự, nghiệp vụ HR, phát hành kỳ lương, xem audit |
| HR | Hồ sơ, tuyển dụng, hợp đồng, checklist, tài sản, duyệt đơn, nhập/duyệt lương demo; không quản trị tài khoản Admin |
| Manager | Thông tin công việc/phê duyệt trong phòng ban, phân bổ và giờ công theo phạm vi; không xem lương nhân viên khác |
| Employee | Hồ sơ của mình, chấm công, đơn từ, kỹ năng, mục tiêu, khóa học, dự án được phân bổ, phiếu lương đã phát hành của mình |

Phạm vi quản lý của bản này dựa trên **phòng ban**, chưa có sơ đồ quản lý trực tiếp nhiều tầng hoặc đa pháp nhân. Một người ở vai trò Manager vẫn không được tự duyệt đơn của chính mình; HR/Admin khác sẽ xử lý.

## 6. Kịch bản trình diễn cho ban tổ chức

### Kịch bản A — một yêu cầu đi qua hai vai trò

1. Admin tạo/cấp tài khoản cho một nhân viên trong phòng ban có Manager.
2. Mở Chrome thường với Admin hoặc Manager; mở cửa sổ ẩn danh với Employee để tránh thay phiên nhau.
3. Nhân viên vào **Nghỉ phép · WFH · OT → Tạo đơn**, chọn ngày và lý do.
4. Manager vào cùng phân hệ, chọn đơn trong phòng ban, duyệt hoặc từ chối. Từ chối phải có lý do.
5. Nhân viên thấy trạng thái thay đổi; hộp thông báo nhận kết quả. Thông báo được làm mới định kỳ tối đa khoảng 30 giây khi đang mở trang, hoặc tải lại để xem ngay.
6. Nếu là nghỉ phép, kiểm tra số phép đang chờ và đã dùng. Nhân viên có thể hủy đơn còn chờ duyệt.

Phép demo mặc định 12 ngày/năm và HR có thể sửa từng người. Tính thứ Hai–thứ Sáu; chưa loại ngày lễ, chưa hỗ trợ nửa ngày/chuyển năm. Đây là tham số demo, không phải kết luận về quyền nghỉ phép theo pháp luật.

### Kịch bản B — phân bổ và giờ công dự án

1. HR/Admin tạo dự án.
2. Phân bổ nhân viên vào dự án, tổng tỷ lệ các dự án tối đa 100%.
3. Nhân viên vào **Giờ công dự án**, ghi ngày, số giờ, mô tả, billable/non-billable.
4. Lưu nháp → gửi duyệt; Manager/HR duyệt. Một ngày không thể ghi quá 24 giờ tổng cộng trên các dự án.
5. Bản ghi đã gửi hoặc duyệt không được nhân viên tự sửa; bị từ chối thì có thể sửa rồi gửi lại.

### Kịch bản C — bảng lương và phiếu lương riêng

1. HR/Admin vào **Lương & phiếu lương**, tạo kỳ theo tháng.
2. Thêm dòng cho nhân viên: lương cơ bản, phụ cấp, thưởng, khoản khấu trừ.
3. Hệ thống tính `Tổng thu nhập = Cơ bản + Phụ cấp + Thưởng`; `Thực nhận = Tổng thu nhập − Khấu trừ`.
4. Duyệt kỳ; **Admin** phát hành. Kỳ đã phát hành không còn sửa/mở lại bằng các API này.
5. Nhân viên đăng nhập, mở **Phiếu lương**, chỉ xem phiếu của mình đã phát hành.
6. Bấm **In / Lưu PDF**, chọn **Save as PDF / Lưu dưới dạng PDF** trong hộp in của trình duyệt.

Các khoản lương/khấu trừ đang nhập thủ công. Chưa có máy tính thuế/BHXH theo thời điểm, chốt công/OT tự đưa vào lương, lệnh chuyển tiền ngân hàng hoặc phê duyệt bắt buộc hai người khác nhau.

### Kịch bản D — tuyển dụng và bàn giao nghỉ việc

- Tuyển dụng: thêm ứng viên → chuyển vòng trên Kanban → đến Offer → tiếp nhận. Hệ thống tạo hồ sơ nhân viên và ba mục onboarding. Admin cấp tài khoản từ hồ sơ đã tạo.
- Nghỉ việc: HR tạo checklist trong tab Offboarding → xác nhận bàn giao → đến ngày làm việc cuối → hoàn tất. Hệ thống ngừng hồ sơ, khóa tài khoản HRM, thu hồi phân bổ/tài sản và hủy đơn đang chờ.
- Tài khoản GitHub/Jira/Google/Slack ở ngoài HRM vẫn phải thu hồi thủ công; mục checklist là xác nhận công việc đã làm, không phải kết nối API đến các dịch vụ đó.

## 7. Cập nhật GitHub và Render sau khi xem bản local

Giữ nguyên repository và Render service hiện có. Chỉ cần đưa mã mới lên cùng repository.

### Kiểm tra và commit

Trong terminal tại `C:\Users\Admin\Desktop\TechHRMS`:

```powershell
git status
```

Kiểm tra chỉ có source/config/docs/test thay đổi. Database, `node_modules`, `bin`, `obj`, `.env`, thư mục publish không được đưa lên GitHub. `.gitignore` đã có các loại trừ này; nếu trước đây từng track file riêng tư, cần xử lý riêng trước khi commit.

Sau khi đã xem bản local:

```powershell
git add .
git commit -m "Upgrade TechHRMS V4 accounts and HR workflows"
git push origin main
```

Lệnh cuối cập nhật repository đang liên kết Render; nếu bật Auto Deploy, Render sẽ bắt đầu triển khai. Workflow kiểm thử trong `.github` không chứa lệnh deploy riêng.

### Trên Render

1. Mở Web Service **techhrms-enterprise** hiện có.
2. Giữ Environment là **Docker**, Root Directory trống, Dockerfile tại `./Dockerfile`, build context gốc repository.
3. Giữ nguyên `ConnectionStrings__Postgres` đang kết nối database và mật khẩu Admin đã cấu hình. Không tạo database khác và không xóa database hiện tại để nâng cấp.
4. Giữ các biến `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` và `SEED_SAMPLE_DATA` theo cấu hình demo trước. Thêm/chỉnh seed không tạo lại tài khoản nếu database đã có dữ liệu; cấp tài khoản bằng trang quản trị mới.
5. Health Check Path tiếp tục là `/api/health/ready`.
6. Chờ Auto Deploy. Nếu chưa bật Auto Deploy, chọn **Manual Deploy → Deploy latest commit**.
7. Khi deploy thành công, mở `https://techhrms-enterprise.onrender.com/`, nhấn `Ctrl+Shift+R`, đăng nhập bằng Admin đang có.
8. Tạo tài khoản demo riêng cho các vai trò, thử kịch bản ở mục 6, rồi mới gửi link cho ban tổ chức.

Trước lần nâng cấp cloud, giữ bản sao dữ liệu cần giữ. Các kiểm thử đã chạy trên database thử nghiệm, không phải database Render thật của bạn. Nâng cấp bổ sung schema không thay thế việc sao lưu dữ liệu quan trọng.

Nếu triển khai báo `CSRF_INVALID`, hãy hard refresh và đăng nhập lại để frontend/backend cùng phiên bản. Trên Render, hệ thống nhận HTTPS từ proxy khi `RENDER=true`; các host khác cần cấu hình proxy tin cậy theo tài liệu API.

## 8. App Windows có phải làm lại không?

**Không cần làm lại bộ cài chỉ để dùng các chức năng web mới**, nếu App hiện tại vẫn mở đúng URL Render ở trên. App sẽ tải giao diện từ URL đó. Sau khi nâng web, đóng/mở lại App hoặc tải lại, đăng nhập lại nếu được yêu cầu.

Gói V4 có thêm mã Desktop hoàn chỉnh, dùng khi bạn muốn có nút thử lại lỗi kết nối và cấu hình URL bằng file. Bản này vẫn là App online dùng chung Backend với Web, không phải hệ thống HR offline.

### Tạo bản Desktop V4 tùy chọn

1. Mở `desktop/desktop-settings.json`; giữ `websiteUrl` là URL Render đang dùng.
2. Nháy đúp `BUILD-DESKTOP.cmd` ở gốc project, hoặc chạy từ PowerShell:

```powershell
.\BUILD-DESKTOP.cmd
```

3. Script publish vào `artifacts/desktop-win-x64`.
4. Chạy thử `TechHRMS.Desktop.exe` trong thư mục đó trên Windows. Máy cần Microsoft Edge WebView2 Runtime; .NET runtime đã được đưa vào output self-contained.
5. Trong Advanced Installer, dùng project `.aip` bạn đã có, cập nhật toàn bộ payload từ `artifacts/desktop-win-x64`. Không chỉ lấy riêng một `.exe` và không lấy thư mục cache `.exe.WebView2`.
6. Cập nhật phiên bản sản phẩm. Giữ UpgradeCode của sản phẩm cũ nếu cấu hình nâng cấp; để Advanced Installer xử lý ProductCode của phiên bản mới theo cấu hình dự án. Nếu đang dùng quy trình demo gỡ bản cũ rồi cài mới, kiểm tra trên máy thử sau khi gỡ.
7. Build MSI như quy trình bạn đã làm, chạy thử cài/mở/đăng nhập. Gói mã nguồn này không chứa `.aip` cũ hoặc MSI đã được kiểm thử trên máy bạn.
8. Tải bộ cài đã kiểm tra lên Google Drive, bật người có link được xem/tải và gửi link cùng tài khoản demo dành cho người thử.

Dữ liệu WebView2 được lưu tại `%LOCALAPPDATA%\TechHRMS\WebView2`, giúp người dùng không cần chạy app bằng quyền Administrator chỉ để sử dụng.

## 9. Khi gặp lỗi

| Dấu hiệu | Việc cần làm |
| --- | --- |
| Không nhận lệnh `dotnet` | Kiểm tra cài .NET SDK 10, mở lại terminal |
| Không nhận lệnh `npm` | Kiểm tra Node.js 24, mở lại terminal |
| `npm ci` lỗi thiếu/khác lockfile | Dùng cả `package.json` và `package-lock.json` trong gói, không chỉ chép `src` |
| Không kết nối Backend | Kiểm tra terminal API và `/api/health/ready`; local cần Backend đang chạy |
| Đăng nhập lại sau nâng cấp | Bình thường ở lần đầu chuyển V3 → V4, dùng mật khẩu cũ |
| Tài khoản có email nhưng không đăng nhập được | Admin cấp tài khoản cho hồ sơ, hoặc đặt lại mật khẩu tài khoản đã có |
| Hết phiên/khóa tài khoản | Giao diện về Login; Admin kiểm tra trạng thái/quyền của tài khoản |
| HTTP 409 | Dữ liệu trùng hoặc vừa có người xử lý; tải lại trước khi thao tác |
| Không thấy menu của HR/Admin | Kiểm tra vai trò đang đăng nhập; không phải mọi tài khoản đều có toàn bộ menu |
| Manager không thấy đơn nhân viên | Kiểm tra hai người thuộc cùng phòng ban |
| Không thấy phiếu lương | Kỳ phải được Admin phát hành, phiếu phải thuộc người đang xem |
| App không tải trang nhưng Chrome tải được | Kiểm tra WebView2 Runtime; dùng nút Tải lại hoặc mở lại App |

## 10. Các phần chưa có

Xem `docs/DOI-CHIEU-YEU-CAU.md` để phân biệt chức năng chạy được và phần cần phát triển tiếp: SSO/MFA, nhiều pháp nhân, quy trình phê duyệt cấu hình nhiều tầng, payroll theo luật, 360°/9-box, tài liệu/CV upload, tích hợp lịch/email/Slack/Jira/ngân hàng, import Excel và report builder. Không có nút giả để coi các tích hợp này là đã hoàn thành.
