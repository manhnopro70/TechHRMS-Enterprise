# Kiem thu TechHRMS V3 (Windows / localhost)

## Kiem tra moi truong

1. Backup ca thu muc `TechHRMS`, bao gom `backend/techhrms.db`.
2. Dung Backend va Frontend V2 truoc khi ghi de patch.
3. Trong VS Code Terminal 1: `cd C:\Users\Admin\Desktop\TechHRMS\backend` → `dotnet restore` → `dotnet build` → `dotnet run --urls http://localhost:5200`.
4. Chrome `http://localhost:5200/api/health/ready` can co `status: ready`.
5. Terminal 2: `cd C:\Users\Admin\Desktop\TechHRMS\frontend` → `npm run dev`.
6. Chrome `http://localhost:5173` → dang nhap tai khoan Local Demo.

## Kiem thu giao dien theo vai tro

- [ ] Admin login, Dashboard hien thi thuc du lieu API, menu 17 trang.
- [ ] Nhap `Ctrl+K`, tim 'cham cong', dieu huong duoc.
- [ ] Mo **Sơ đồ tổ chức**, thay so luong theo phong ban.
- [ ] Mo **Hợp đồng**, tao hop dong `DEMO-HD-002` cho nhan vien gia, cap nhat trang thai.
- [ ] Mo **Onboarding**, tao checklist, danh dau hoan thanh, tai lai trang van con.
- [ ] Mo **Chấm công**, chon Hybrid, check-in, check-in lan hai bi tu choi, check-out, tai lai van co lich su.
- [ ] Mo **Đào tạo**, dang ky khoa hoc, chuyen qua Dang hoc / Hoan thanh, tai lai van co.
- [ ] Mo **Báo cáo**, so lieu headcount phu hop voi danh sach nhan su; so 'chi phi' khong bi bia.
- [ ] Mo cac module cu: nhan su, don tu, skill, du an, asset, ATS, OKR; thao tac save/reload.
- [ ] Logout, login Employee, chi xem hop dong va don tu cua chinh minh.
- [ ] Login Manager, chi xem team cua minh trong attendance/onboarding/insights.
- [ ] Thu direct API POST `/api/contracts` tu Employee: phai 403.
- [ ] Chua login truy cap `/api/contracts`: phai 401.
- [ ] Thu tren mobile Chrome devtools: 390px va 768px, menu dung duoc, bang cuon ngang khong vo bo cuc.

## Bat buoc truoc public

- [ ] Khong co du lieu PII that; khong commit `backend/techhrms.db`.
- [ ] Khong public email/password demo mac dinh; su dung secret/tai khoan moi.
- [ ] Them EF Core migrations versioned; khong dùng EnsureCreated / DDL tay khi lam production.
- [ ] Kiem tra CSRF, same-origin, secure cookies, role abuse, race conditions, rate limit, password reset, session revocation.
- [ ] Database backup/restore test, error and audit logging, secrets/retention review.
- [ ] Docker build, startup PostgreSQL, login, persistence after restart and backup restore test.
