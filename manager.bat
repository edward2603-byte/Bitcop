@echo off
chcp 65001 >nul
setlocal
title WorkControl - локальный запуск
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo Не найден .NET SDK 10.
  echo Установите .NET 10 SDK с https://dotnet.microsoft.com/download/dotnet/10.0
  echo Затем закройте это окно и снова запустите manager.bat.
  pause
  exit /b 1
)

dotnet --list-sdks | findstr /B /C:"10.0.4" >nul
if errorlevel 1 (
  echo Не найден .NET SDK 10.0.404 или более новый патч ветки 10.0.4xx.
  echo Скачайте .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
  pause
  exit /b 1
)

where node >nul 2>nul
if errorlevel 1 (
  echo Не найден Node.js 20 или новее.
  echo Установите LTS-версию Node.js с https://nodejs.org/
  echo Затем закройте это окно и снова запустите manager.bat.
  pause
  exit /b 1
)

for /f %%V in ('node -p "process.versions.node.split('.')[0]"') do set "NODE_MAJOR=%%V"
if not defined NODE_MAJOR (
  echo Не удалось определить версию Node.js.
  pause
  exit /b 1
)
if %NODE_MAJOR% LSS 20 (
  echo Установлена устаревшая версия Node.js. Требуется Node.js 20 или новее.
  echo Скачайте LTS-версию: https://nodejs.org/
  pause
  exit /b 1
)

where npm >nul 2>nul
if errorlevel 1 (
  echo Не найден npm. Переустановите Node.js 20 или новее вместе с npm.
  pause
  exit /b 1
)

echo Проверка серверных компонентов...
call dotnet restore "src\server\WorkControl.Api\WorkControl.Api.csproj"
if errorlevel 1 goto :restore_error

echo Подготовка тестового пакета Windows-агента...
call dotnet publish "src\agent\WorkControl.Agent\WorkControl.Agent.csproj" -c Debug --self-contained false -p:UseAppHost=false -o "release\agent"
if errorlevel 1 goto :agent_error
copy /Y "src\agent\WorkControl.Agent\employee.bat" "release\agent\employee.bat" >nul

if not exist "src\web\node_modules" (
  echo Установка веб-компонентов. При первом запуске это может занять несколько минут...
  call npm --prefix "src\web" ci --no-audit --no-fund
  if errorlevel 1 goto :web_error
)

echo Запуск API только на текущем компьютере: http://127.0.0.1:5080
start "WorkControl API - локально" /D "%~dp0" cmd /k "dotnet run --no-restore --project src\server\WorkControl.Api --urls http://127.0.0.1:5080"
timeout /t 3 /nobreak >nul

echo Запуск панели только на текущем компьютере: http://127.0.0.1:5173
start "WorkControl Web - локально" /D "%~dp0src\web" cmd /k "npm run dev -- --host 127.0.0.1"
timeout /t 2 /nobreak >nul
start "" "http://127.0.0.1:5173"
exit /b 0

:restore_error
echo Не удалось восстановить пакеты API. Проверьте подключение к интернету и повторите запуск.
pause
exit /b 1

:agent_error
echo Не удалось собрать тестовый пакет Windows-агента.
echo Подробности ошибки показаны выше. Запуск остановлен.
pause
exit /b 1

:web_error
echo Не удалось установить веб-компоненты. Проверьте подключение к интернету и повторите запуск.
pause
exit /b 1
