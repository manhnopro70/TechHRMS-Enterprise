# TechHRMS Enterprise V3.1 - Bản chỉnh giao diện

## Nội dung thay đổi

- Loại tên cá nhân khỏi lời chào Dashboard và thanh trên cùng. **Không xóa dữ liệu trong Database.**
- Chuyển nút **Tìm phân hệ / Thông báo / Đăng xuất** từ góc phải trên xuống nhóm **CÔNG CỤ NHANH** ở **cuối sidebar bên trái**.
- Giao diện màu sắc thống nhất: xanh navy đậm + xanh ngọc, vàng ấm dùng có chọn lọc.
- Giữ nguyên các trang nghiệp vụ, lời gọi API và phân quyền hiện tại.

## Điều kiện

Đây là bản **vá cho Enterprise V3**. Trước khi dùng, thư mục sau phải tồn tại:

`C:\Users\Admin\Desktop\TechHRMS\frontend\src\EnterprisePages.tsx`

và `frontend/src/enterprise.css`, `frontend/src/premium.css`.

## Cài đặt an toàn trên Windows

1. Dừng Vite và Backend nếu đang chạy (`Ctrl + C` trong từng Terminal). Tắt VS Code nếu muốn.
2. Sao lưu toàn bộ `C:\Users\Admin\Desktop\TechHRMS` thành `TechHRMS_Backup_Truoc_V31` trên Desktop.
3. Giải nén file ZIP. Mở bên trong tới thư mục có `frontend` và `HUONG-DAN.md`.
4. Copy **thư mục `frontend`** trong gói nâng cấp, dán vào **bên trong** `C:\Users\Admin\Desktop\TechHRMS`.
5. Khi Windows hỏi có thay thế file `App.tsx` không, chọn **Replace the files in the destination**. Chỉ có hai file dưới đây được đưa vào `frontend/src`:
   - `App.tsx` (ghi đè)
   - `refined.css` (tạo mới)
6. Không xóa `frontend/node_modules`, `package.json`, `backend` hoặc `backend/techhrms.db`.
7. Mở VS Code > File > Open Folder > `C:\Users\Admin\Desktop\TechHRMS`.

## Chạy kiểm tra

Mở Terminal 1 (Backend):

```powershell
cd C:\Users\Admin\Desktop\TechHRMS\backend
dotnet run --urls http://localhost:5200
```

Giữ Terminal 1 đang chạy. Mở Terminal 2 (Frontend):

```powershell
cd C:\Users\Admin\Desktop\TechHRMS\frontend
npm run dev
```

Mở Chrome tại `http://localhost:5173`, đăng nhập bằng tài khoản local bạn đã dùng.

Kiểm tra:
- Dashboard không còn tên cá nhân trong lời chào.
- Header góc phải chỉ có **Không gian Quản trị/HR/Quản lý/Nhân viên – DEMO**.
- Dưới cuối sidebar có nhóm **Công cụ nhanh**: Tìm phân hệ, Thông báo, Đăng xuất.
- Nhấn `Ctrl + K` để tìm chức năng; thử nút Thông báo; sang Nhân sự hoặc Đơn từ để kiểm tra dữ liệu API.
- Trên điện thoại: mở menu `☰` rồi dùng các nút trong sidebar.

## Lưu ý

- Đây là **bản chỉnh giao diện**, không phải bản sửa Backend hoặc chức năng tính lương.
- Preview HTML dùng dữ liệu minh họa, không kết nối Database.
- Nếu Chrome chưa cập nhật giao diện, nhấn `Ctrl + Shift + R`.
- Nếu thấy lỗi `Cannot find module './EnterprisePages'` hoặc `enterprise.css`, bạn chưa ghép bản Enterprise V3 đầy đủ; hãy khôi phục bản sao và bổ sung V3 trước khi dùng V3.1.
