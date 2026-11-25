@echo off
setlocal EnableExtensions DisableDelayedExpansion
cd /d "%~dp0"

"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\bootstrap-admin.ps1"
set "BOOTSTRAP_EXIT=%ERRORLEVEL%"

if not "%BOOTSTRAP_EXIT%"=="0" (
  echo Administrator bootstrap did not complete. See the messages above.
  pause
  exit /b %BOOTSTRAP_EXIT%
)

echo Administrator bootstrap completed. You can now start manager.bat.
pause
exit /b 0
