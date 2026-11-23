@echo off
setlocal
cd /d "%~dp0"
dotnet publish "desktop\TechHRMS.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "artifacts\desktop-win-x64"
if errorlevel 1 (
  echo.
  echo Build failed. Please keep this window open and copy the error.
  pause
  exit /b 1
)
echo.
echo Published: %CD%\artifacts\desktop-win-x64
echo Include ALL files in this folder in your Advanced Installer package.
explorer "artifacts\desktop-win-x64"
pause
