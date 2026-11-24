@echo off
setlocal
title WorkControl - Manager dashboard
cd /d "%~dp0"

fltmc >nul 2>&1
if errorlevel 1 (
  echo Windows permission is required to allow pilot agents on the private network.
  powershell -NoProfile -Command "Start-Process -FilePath '%ComSpec%' -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
  exit /b 0
)

powershell -NoProfile -Command "if (-not (Get-NetFirewallRule -DisplayName 'WorkControl API TCP 5080' -ErrorAction SilentlyContinue)) { New-NetFirewallRule -DisplayName 'WorkControl API TCP 5080' -Description 'Local WorkControl API for pilot agents' -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5080 -Profile Private | Out-Null }"

where npm >nul 2>nul
if errorlevel 1 (
  echo Missing Node.js 20 or later. Install it, then run this file again.
  pause
  exit /b 1
)

if not exist "src\web\node_modules" (
  echo Installing web components. This happens only on first run.
  call npm --prefix "src\web" install --no-audit --no-fund
  if errorlevel 1 (
    echo Web component installation failed.
    pause
    exit /b 1
  )
)

if exist ".tools\dotnet\dotnet.exe" if not exist "release\agent\WorkControl.Agent.dll" (
  echo Preparing script-based employee agent. This happens only once.
  call ".tools\dotnet\dotnet.exe" publish "src\agent\WorkControl.Agent\WorkControl.Agent.csproj" -c Debug --self-contained false -p:UseAppHost=false -o "release\agent"
  if errorlevel 1 (
    echo Agent package preparation failed. The dashboard can still be used.
  ) else (
    copy /Y "src\agent\WorkControl.Agent\employee.bat" "release\agent\employee.bat" >nul
  )
)

if exist ".tools\dotnet\dotnet.exe" (
  start "WorkControl API" /D "%~dp0" cmd /k ".tools\dotnet\dotnet.exe run --project src\server\WorkControl.Api --urls http://0.0.0.0:5080"
  timeout /t 3 /nobreak >nul
)

echo Opening WorkControl dashboard in browser...
start "WorkControl Web" /D "%~dp0src\web" cmd /k "npm run dev -- --host 127.0.0.1"
timeout /t 2 /nobreak >nul
start "" "http://localhost:5173"
exit /b 0
