# TechHRMS Enterprise V3 - upgrade tu Premium V2

TechHRMS V3 **khong phai production ready**. Day la ban nang cap demo co workflow that qua React → ASP.NET Core → SQLite/PostgreSQL, danh cho Project `C:\Users\Admin\Desktop\TechHRMS` da cai ban Fullstack Starter + Premium V2.

## Cai ban V3 an toan

1. Nhan `Ctrl+C` o **hai** terminal dang chay Vite va ASP.NET Core.
2. Copy toan bo `C:\Users\Admin\Desktop\TechHRMS` sang `C:\Users\Admin\Desktop\TechHRMS_Backup_Truoc_V3`.
3. Tai ZIP `TechHRMS-Enterprise-V3-Upgrade.zip`, click phai **Extract All**.
4. Mo **ben trong** thu muc giai nen cho den khi thay `backend`, `frontend`, `docs`.
5. Chon tat ca cac muc *ben trong* va copy vao `C:\Users\Admin\Desktop\TechHRMS` → Replace khi Windows hoi.
6. **Khong xoa hoac ghi de** `backend/techhrms.db`, `frontend/node_modules`, `frontend/package.json`, `frontend/package-lock.json`.
7. Trong VS Code kiem tra co file `frontend/src/enterprise.css`, `frontend/src/EnterprisePages.tsx`, `backend/EnterpriseSchema.cs`, `backend/Endpoints.Enterprise.cs`.

### Terminal 1 (Backend)

```powershell
cd C:\Users\Admin\Desktop\TechHRMS\backend
dotnet restore
dotnet build
dotnet run --urls http://localhost:5200
```

### Terminal 2 (Frontend)

```powershell
cd C:\Users\Admin\Desktop\TechHRMS\frontend
npm run dev
```

Chrome `http://localhost:5173`. Backend readiness: `http://localhost:5200/api/health/ready`.

### Tai khoan local DEMO (chi dung tren may)

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@techhrms.local` | `LocalDemo@2026!` |
| HR | `hr@techhrms.local` | `LocalDemo@2026!` |
| Manager | `manager@techhrms.local` | `LocalDemo@2026!` |
| Employee | `employee@techhrms.local` | `LocalDemo@2026!` |

Khong dung mat khau nay de public. Mua/luong/CCCD: KHONG nhap du lieu that.

## V3 co gi moi?

- New V3 Iris Design System (light shell, categories in sidebar, keyboard search, responsive, task-first dashboard).
- New end-to-end modules: contracts basic, onboarding checklists, attendance self-check-in/out, training courses + enrollment, department overview, scoped HR insights.
- New API routes: `GET /api/organization`, `GET/POST /api/contracts`, `PUT /api/contracts/{id}/status`, `GET/POST /api/onboarding`, `PUT /api/onboarding/{id}/status`, `GET /api/attendance`, `POST /api/attendance/check-in`, `POST /api/attendance/check-out`, `GET/POST /api/courses`, `GET /api/enrollments`, `POST /api/courses/{id}/enroll`, `PUT /api/enrollments/{id}/status`, `GET /api/insights`.
- Cac bang du lieu moi them theo huong **additive**: `EmploymentContracts`, `OnboardingTasks`, `TimeEntries`, `TrainingCourses`, `TrainingEnrollments`.
- V3 khong sua bang du lieu cu, khong phat hanh migrations. `EnterpriseSchema.cs` tao bang moi tren local SQLite va Postgres; chi dung cho demo.
- Giữ nguyên API/module cũ: employees, departments, work requests, skills, resources, assets, candidates, objectives, audit.
- `docs/DOI-CHIEU-YEU-CAU.md`: bang so sanh day du voi phan tich nghiep vu.

## Gioi han / tinh trung thuc

Phan quyen hien tai co **4 vai tro**, chua co Finance, IT Admin rieng, C-level va Super Admin phan cap. Chua tinh luong/bao hiem/thue/chot cong, chua HRIS multi-legal-entity, chưa co SSO/MFA, GPS proof, e-sign, ATS lich phong van, workflow engine, import Excel hay chu ky danh gia 360. Xem bang doi chieu.

**Kiem thu da thuc hien o moi truong tao ma**: TS/TSX syntax + type-check voi shim, SQLite DDL + unique indexes, kiem tra ZIP. Khong co .NET SDK va NPM registry de build end-to-end trong moi truong nay; *ban phai chay* `dotnet build` va kiem thu thao tac tren Windows truoc khi coi la hoan thanh.

## Deploy sau khi kiem thu

Co the giu Dockerfile V2 (build React va ASP.NET Core chung mot image), nhung can versioned EF Migrations, PostgreSQL bền vững, secrets, HTTPS, kiem tra phan quyen, backup va XSS/CSRF truoc khi gui link. Tu choi su dung PII that tren demo public.
