@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\bootstrap-admin.ps1"
if errorlevel 1 (
  echo Не удалось создать администратора. Подробности показаны выше.
  pause
  exit /b 1
)
echo Готово. Теперь можно запустить manager.bat.
pause
