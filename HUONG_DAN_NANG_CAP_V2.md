> Tài liệu lịch sử của bản cũ. Với gói V4, làm theo `HUONG-DAN.md` ở gốc project.

# TechHRMS Premium v2.0 - Upgrade overlay

This is an upgrade for the existing TechHRMS Fullstack Starter project, not a complete stand-alone project.

## Prerequisite

The existing project has `frontend/` and `backend/` from the TechHRMS Fullstack Starter and runs on Windows with Node.js and .NET 10 SDK.

## Windows installation

1. Stop the two local processes (Vite and ASP.NET Core) with Ctrl+C.
2. Back up the entire folder: `C:\Users\Admin\Desktop\TechHRMS` to `TechHRMS_Backup`.
3. Extract `TechHRMS-Premium-V2-Upgrade.zip`.
4. Open the extracted folder; copy **its contents** (`frontend`, `backend`, and this README).
5. Paste them into `C:\Users\Admin\Desktop\TechHRMS` and select **Replace files in destination** when prompted.
6. DO NOT delete the existing `frontend/package.json`, `frontend/package-lock.json`, or `frontend/node_modules`.
7. DO NOT delete or overwrite the existing `backend/techhrms.db`; the upgrade does not contain a database file.
8. Run the following two command sequences in **separate** terminals:

```powershell
cd C:\Users\Admin\Desktop\TechHRMS\backend
dotnet restore
dotnet build
dotnet run --urls http://localhost:5200
```

```powershell
cd C:\Users\Admin\Desktop\TechHRMS\frontend
npm run dev
```

9. Open `http://localhost:5173` and log in with your existing local demo account.
10. Check `http://localhost:5200/api/health/ready`; it should report `status: ready` and `database: connected`.

## What changed

- A premium midnight / mint design theme with responsive screens and subtle animations.
- Dashboard based on actual API counts, workforce distribution, remote-work share, and recent requests.
- Role-sensitive left navigation, a working keyboard navigation panel (Ctrl+K), and a request inbox icon.
- A new SVG favicon and updated login visual styling.
- The original forms and API endpoints remain in place.
- Updated `SQLitePCLRaw.bundle_e_sqlite3` to `2.1.14`.
- Corrected nullable string access in employee and objective endpoints.
- Added production exception handling and common response security headers.
- Added `/api/health/ready` as a database readiness check.

## Important notes

- Existing React App.css is **still required** and loaded before premium.css.
- The original business logic remains a demo and is NOT production ready. Real deployments need tests, security review, database migrations, backups, role/permission audits, logging, and realistic data-retention rules.
- The screenshot preview is illustrative and uses fictional counts. The running dashboard loads data from your own backend.
- The production hosting configuration should be verified separately before public deployment.
- This overlay does not include and does not require a real employee database.
