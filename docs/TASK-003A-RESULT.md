# TASK-003A — результат обновления платформы

Дата проверки: 24.11.2026. Базовый коммит: `a6c07cc87755863e022867a6f87e5a30b7fd451e`.

## Выполнено

- API переведён с `net8.0` на `net10.0`.
- WPF-агент и тесты переведены с `net8.0-windows` на `net10.0-windows`.
- SDK закреплён в `global.json`: `10.0.404`, `rollForward=latestPatch`, предварительные версии запрещены.
- Microsoft EF Core, ASP.NET Core Testing и SQLite закреплены на `10.0.15`; Npgsql EF — на `10.0.4`; Microsoft.NET.Test.Sdk — на `18.12.0`.
- GitHub Actions, `manager.bat`, `employee.bat` и пользовательские инструкции обновлены для .NET 10.
- Бизнес-логика, схема данных, Identity, сетевые привязки и правила брандмауэра не изменялись.

## Затронутые файлы

- `global.json`;
- `src/server/WorkControl.Api/WorkControl.Api.csproj`;
- `src/agent/WorkControl.Agent/WorkControl.Agent.csproj`;
- `src/agent/WorkControl.Agent/employee.bat`;
- `tests/WorkControl.Api.Tests/WorkControl.Api.Tests.csproj`;
- `.github/workflows/verify.yml`;
- `manager.bat`;
- `README.md`;
- `docs/DEVELOPMENT.md`;
- `docs/FIRST_RUN_CHECKLIST.md`;
- `docs/MODULE_INDEX.md`;
- `docs/TASK-003A-RESULT.md`.

Входной комплект задания сохранён в `docs/WorkControl_TASK-003_FULL.md` без изменения содержания.

## Версии

| Компонент | Версия |
| --- | --- |
| .NET SDK | `10.0.404` |
| .NET / ASP.NET / Windows Desktop Runtime | `10.0.15` |
| EF Core Design / SQLite | `10.0.15` |
| Npgsql.EntityFrameworkCore.PostgreSQL | `10.0.4` |
| Microsoft.Data.Sqlite | `10.0.15` |
| Microsoft.AspNetCore.Mvc.Testing | `10.0.15` |
| Microsoft.NET.Test.Sdk | `18.12.0` |
| Node.js / npm | `20.20.2` / `10.8.2` |

Официальный статус .NET 10: LTS до ноября 2028 года. Версии пакетов сверены по NuGet перед обновлением. Источники: [поддержка .NET](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support), [установка .NET на Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows), [NuGet Gallery](https://www.nuget.org/).

## Сохранение SQLite

До изменения проектов создана локальная резервная копия:

`backups/task-003a/workcontrol-pre-net10-20261124-1600.db`

SHA-256 исходной базы и копии на момент создания:

`458FF40B24EF10FAC5B329B542A8412D3421923BE1FB2B81376A99DB147AA44F`

После сборки API .NET 10 был отдельно запущен с автоматической миграцией, отключённой через `Database__AutoMigrate=false`, сначала на резервной, затем на рабочей базе. Ответы `/api/dashboard` совпали полностью: прочитаны те же три устройства и те же значения времени/активности. Хэши файлов до LOCAL smoke-test также совпали.

Новая EF migration не создавалась: TASK-003A не меняет модель или схему. Для SQLite сохранён текущий `EnsureCreated`, для PostgreSQL — существующий путь `Migrate`; разрушительных операций не выполнялось.

## Проверки до изменения

- `dotnet test ... -c Release`: 10 из 10 успешно на .NET 8.
- `npm test`: 3 из 3 успешно.
- `npm run build`: успешно.

## Проверки после изменения

- `dotnet restore tests/WorkControl.Api.Tests/WorkControl.Api.Tests.csproj`: успешно.
- `dotnet build` API Release: успешно, 0 предупреждений, 0 ошибок.
- `dotnet build` Windows-агента Release: успешно, 0 предупреждений, 0 ошибок.
- `dotnet test` Release: 10 из 10 успешно на `net10.0`.
- `dotnet publish` Windows-агента: успешно; пакет создан в `release/agent`.
- Запуск на новой пустой SQLite-базе: таблицы и индексы созданы, `health=ok`, dashboard содержит 0 устройств до регистрации агента.
- `dotnet list package --vulnerable --include-transitive`: известных уязвимых NuGet-пакетов для текущего источника не обнаружено.
- `npm ci`: успешно. Первый параллельный запуск завершился системной ошибкой памяти `esbuild`; отдельный повтор прошёл.
- `npm test`: 3 из 3 успешно.
- `npm run build`: успешно.
- `manager.bat`: restore и publish успешны; API ответил `ok`, web вернул HTTP 200.
- Слушатели LOCAL были только на `127.0.0.1:5080` и `127.0.0.1:5173`; после теста остановлены.
- WPF-агент из пакета .NET 10 запустился, зарегистрировал текущий тестовый компьютер и доставил событие. Dashboard показал статус «На связи» и состояние «Активен».

Функциональная проверка выполнена на Windows 10 Pro `10.0.19045`. Визуальное ручное подтверждение WPF на Windows 11 не выполнялось; сборка для `net10.0-windows` подтверждена.

## Ограничения и непроверенные пункты

- Реальный PostgreSQL-сервер не поднимался; совместимость Npgsql подтверждена restore/build, но runtime/migration на PostgreSQL не проверены.
- Windows 10 Pro build 19045 фактически прошла LOCAL-проверку, но текущая официальная таблица .NET 10 ограничивает поддержку Windows 10 вариантами LTSC/Enterprise. Для пилота следует использовать поддерживаемую редакцию Windows или Windows 11; статус ОС необходимо подтвердить отдельно.
- Визуальная компоновка WPF после запуска не проверена автоматизацией: средство управления окнами было недоступно. Запуск процесса и передача события подтверждены.
- GitHub Actions следует проверить по результату CI после отправки коммита.
- Сетевой режим, Identity, аутентификация агентов, HTTPS и TASK-003B–003E не реализовывались.
- Массовая установка и реальные данные сотрудников по-прежнему запрещены.

## Итог

TASK-003A выполнена локально: поддерживаемая платформа .NET 10 закреплена, сборки и тесты проходят, старые SQLite-данные читаются без потери, LOCAL-цепочка агент → API → БД → web сохранена. Переход к TASK-003B возможен только после отдельного задания и приёмки этого результата.
