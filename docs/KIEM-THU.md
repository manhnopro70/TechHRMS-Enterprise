# Checklist kiểm thử thủ công TechHRMS v1.0

## A. Khởi chạy

- [ ] `dotnet restore` tải dependencies thành công.
- [ ] `dotnet run --urls http://localhost:5200` bắt đầu không lỗi.
- [ ] `http://localhost:5200/api/health` trả JSON status ok.
- [ ] `npm run dev` trong frontend chạy thành công.
- [ ] `http://localhost:5173` hiển thị trang login, không hiển thị lỗi Vite.

## B. Auth/Permissions

- [ ] Mật khẩu sai -> HTTP 401 ở `/api/auth/login`, UI báo lỗi.
- [ ] Admin login -> có màn hình Nhật ký.
- [ ] HR login -> có Tuyển dụng ATS, không có Nhật ký.
- [ ] Manager login -> chỉ xem nhân viên cùng phòng ban, không có Tuyển dụng.
- [ ] Employee login -> chỉ xem hồ sơ/skill/tài sản/đơn của bản thân.
- [ ] Employee truy cập trực tiếp `/api/candidates` -> HTTP 403, không được data.
- [ ] Employee không thể PUT `/api/employees/{id}` dù gửi trực tiếp API.
- [ ] Manager không thể duyệt đơn của bản thân.
- [ ] Manager không thể duyệt đơn của nhân viên khác phòng ban.
- [ ] Logout -> gọi `/api/auth/me` nhận HTTP 401.

## C. Dữ liệu

- [ ] Admin/HR tạo nhân viên mới; refresh trang vẫn có.
- [ ] Admin/HR sửa email/chức danh; refresh vẫn đúng.
- [ ] Tạo phòng ban với mã trùng -> API báo lỗi.
- [ ] Employee tạo yêu cầu WFH; Manager duyệt; Employee thấy đã duyệt.
- [ ] Employee thêm kỹ năng; HR có thể tìm bằng tên/công nghệ.
- [ ] Asset đổi người nhận, dữ liệu lưu vào database.
- [ ] Cấp phát hai dự án có tổng 110% cho một người -> từ chối.
- [ ] HR chuyển vòng ứng viên; refresh còn trạng thái mới.
- [ ] Cập nhật tiến độ OKR 0..100; ngoài phạm vi bị từ chối.
- [ ] Audit ghi hành động quan trọng.
- [ ] Export CSV mở được bằng Excel, có tiếng Việt.

## D. Responsive và UX

- [ ] 1366px desktop: sidebar và bảng hiển thị ổn.
- [ ] 390px mobile: mở/đóng hamburger menu, form không tràn màn hình.
- [ ] Lỗi Backend mất kết nối hiển thị cảnh báo rõ ràng.
- [ ] Logout/login lại với vai trò khác đúng Portal.

## E. Deploy

- [ ] Docker build hoàn thành.
- [ ] Render Web Service chạy HTTPS và giữ một domain.
- [ ] `ConnectionStrings__Postgres` chỉ nằm trong host secret.
- [ ] Sau restart, dữ liệu lưu ở PostgreSQL vẫn còn.
- [ ] Không có file `techhrms.db`, `.env`, `bin`, `obj`, `node_modules` trong git.
- [ ] Không dùng thông tin nhân sự thực trên demo công khai.

> Đây là checklist để người phát triển **thực hiện sau khi chạy**, không phải bằng chứng hệ thống đã qua tất cả các ca kiểm thử.
