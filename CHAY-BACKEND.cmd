@echo off
cd /d "%~dp0backend"
set "ASPNETCORE_ENVIRONMENT=Development"
echo [TechHRMS] Starting ASP.NET Core API on http://localhost:5200
if not exist TechHRMS.Api.csproj (
  echo Missing backend files.
  pause
  exit /b 1
)
dotnet run --urls http://localhost:5200
pause
