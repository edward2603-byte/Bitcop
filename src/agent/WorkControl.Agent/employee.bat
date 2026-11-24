@echo off
setlocal
title WorkControl - Employee agent
cd /d "%~dp0"

if exist "%~dp0WorkControl.Agent.dll" (
  where dotnet >nul 2>nul
  if errorlevel 1 (
    echo Missing .NET 8 Desktop Runtime.
    echo Install it on this computer, then run employee.bat again.
    pause
    exit /b 1
  )
  dotnet --list-runtimes | findstr /C:"Microsoft.WindowsDesktop.App 8." >nul
  if errorlevel 1 (
    echo Missing .NET 8 Desktop Runtime.
    echo Install it on this computer, then run employee.bat again.
    pause
    exit /b 1
  )
  start "" dotnet "%~dp0WorkControl.Agent.dll"
  exit /b 0
)

echo Agent package was not found.
pause
exit /b 1
