# Разработка WorkControl

## Зависимости

- .NET SDK 10.0.404 (ветка 10.0.4xx);
- Node.js 20+ и npm;
- Windows для сборки и запуска WPF-агента.

## Сборка и тесты

```powershell
dotnet restore tests\WorkControl.Api.Tests\WorkControl.Api.Tests.csproj
dotnet build tests\WorkControl.Api.Tests\WorkControl.Api.Tests.csproj -m:1
dotnet test tests\WorkControl.Api.Tests\WorkControl.Api.Tests.csproj -m:1
npm --prefix src\web ci --no-audit --no-fund
npm --prefix src\web test
npm --prefix src\web run build
dotnet publish src\agent\WorkControl.Agent\WorkControl.Agent.csproj -c Release --self-contained false -p:UseAppHost=false -o release\agent
```

`-m:1` необязателен в CI; он полезен на текущей Windows-машине, где параллельный дочерний узел MSBuild однажды завершился аварийно без ошибки исходного кода.

## База данных

Runtime использует `Database.Migrate`, а не `EnsureDeleted`/`EnsureCreated`. Для SQLite-базы TASK-002 без таблицы истории выполняется одноразовая безопасная baseline-регистрация старой миграции, после чего применяется `SecurePilot`. Существующие устройства получают статус `Active`, события и связи сохраняются; для дальнейшей отправки старый агент необходимо переподключить новым одноразовым кодом.

Перед изменением схемы:

1. остановить API;
2. скопировать SQLite-файл в `backups` (каталог игнорируется Git);
3. зафиксировать SHA-256;
4. проверить чистую базу и обновление копии существующей базы;
5. не помечать PostgreSQL успешным без реального экземпляра.

Тесты обязаны задавать connection string через ранний `UseSetting`; поздний `ConfigureAppConfiguration` нельзя использовать для параметров, читаемых в top-level startup.

## Локальный ADMIN

```powershell
.\bootstrap-admin.bat
```

Или CLI: задать `WORKCONTROL_BOOTSTRAP_EMAIL`, `WORKCONTROL_BOOTSTRAP_PASSWORD` только в текущем процессе и выполнить `dotnet run --project src\server\WorkControl.Api -- --bootstrap-admin`. Процедура прекращает работу, если пользователь уже существует.

## Контуры безопасности

- browser: Identity cookie + CSRF;
- agent: отдельная схема `WorkControl KeyId.Secret`, DeviceId берётся только из claims;
- сервер хранит хэш секрета, агент — DPAPI CurrentUser;
- ADMIN endpoints требуют role policy; dashboard ограничивает группы в `IQueryable` до выполнения SQL;
- LOCAL принимает HTTP только на loopback; агент принимает сетевой адрес только с HTTPS;
- PILOT обслуживает web и API с одного origin и не стартует без обязательных параметров.

Секреты, PFX, реальные данные, runtime-БД, Data Protection keys и DPAPI-файл не коммитить.

## Ограничения

UI управления пользователями/группами и автоматическая плановая ротация агента не реализованы. Нагрузочное испытание и реальный PostgreSQL/PILOT требуют отдельной инфраструктуры.
