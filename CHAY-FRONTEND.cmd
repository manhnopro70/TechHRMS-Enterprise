@echo off
cd /d "%~dp0frontend"
echo [TechHRMS] Starting React/Vite frontend...
if not exist node_modules (
  echo node_modules is missing. Run npm install first.
  pause
  exit /b 1
)
npm run dev
pause
