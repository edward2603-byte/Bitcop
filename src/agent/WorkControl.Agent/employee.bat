@echo off
chcp 65001 >nul
setlocal
title WorkControl - тестовый агент
cd /d "%~dp0"

if exist "%~dp0WorkControl.Agent.dll" (
  where dotnet >nul 2>nul
  if errorlevel 1 (
    echo Не найдена среда выполнения .NET 10 Desktop Runtime.
    echo Установите её с https://dotnet.microsoft.com/download/dotnet/10.0 и снова запустите employee.bat.
    pause
    exit /b 1
  )
  dotnet --list-runtimes | findstr /C:"Microsoft.WindowsDesktop.App 10." >nul
  if errorlevel 1 (
    echo Не найдена среда выполнения .NET 10 Desktop Runtime.
    echo Установите её с https://dotnet.microsoft.com/download/dotnet/10.0 и снова запустите employee.bat.
    pause
    exit /b 1
  )
  start "" dotnet "%~dp0WorkControl.Agent.dll"
  exit /b 0
)

echo Пакет агента не найден. Сначала запустите manager.bat в корне проекта.
pause
exit /b 1
