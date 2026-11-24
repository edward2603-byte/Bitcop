# WorkControl — единый полный комплект TASK-003 для Codex

**Формат:** единый Markdown-документ вместо архива из нескольких файлов.  
**Исходный репозиторий:** `edward2603-byte/Bitcop`.  
**Базовый коммит:** `a6c07cc87755863e022867a6f87e5a30b7fd451e`.  
**Содержимое:** стартовая инструкция, все пять подзадач TASK-003A–003E, полный технический проект, целевая структура каталогов, модели и API, примеры C#, сценарии тестирования и приёмки.

> **Статус:** это технический проект и комплект заданий, а не утверждение о готовности программного кода. Проектные примеры C# требуют интеграции и проверки Codex.

## Как использовать один файл и экономить токены

1. Положите этот файл в репозиторий как `docs/TASK-003_FULL.md`.
2. **Не просите Codex прочитать файл целиком.** В одном файле находятся все этапы; они отделены поисковыми маркерами `BEGIN_TASK_003A` / `END_TASK_003A` и так далее. Codex может прочитать только нужный фрагмент и отдельные разделы технического проекта.
3. Сначала выполните только TASK-003A. После проверки и принятия изменений переходите последовательно к 003B, 003C, 003D и 003E. Одна задача — один коммит/PR и своя проверка.
4. До внесения изменений Codex должен прочитать корневой `AGENTS.md`, `docs/MODULE_INDEX.md`, выбранную секцию ниже и 3–8 релевантных исходников. Другие модули не трогать без необходимости.
5. Технический проект расположен в конце этого же файла между маркерами `BEGIN_DETAILED_DESIGN` / `END_DETAILED_DESIGN`. Он содержит примеры кода и требования по безопасности. Выбирать из него только необходимое.
6. **Никогда не открывать сетевой доступ автоматически.** TASK-003C тестировать на доверенном локальном HTTPS; сетевой PILOT — только после TASK-003D и технического допуска.
7. Любые старые названия `TASK-003A.md`, `TASK-003_DETAILED_DESIGN.md` и другие ссылки из исходного комплекта обозначают соответствующие **секции этого единого файла**, а не отдельные необходимые вложения.

## Карта этапов

| Секция | Что делает Codex | Результат |
|---|---|---|
| TASK-003A | Переход .NET 8 → .NET 10, сборки, миграции без потери данных | Проверенный локальный запуск |
| TASK-003B | ASP.NET Core Identity, вход, роли, разграничение групп | Нет анонимного доступа к панели |
| TASK-003C | Регистрация/одобрение агентов, отдельные секреты и отзыв | Нет анонимной телеметрии |
| TASK-003D | Сертификаты, HTTPS, изолированный сетевой PILOT | Защищённый тест двух ПК |
| TASK-003E | Полная матрица тестов, аудит, восстановление и документация | Проверенный ограниченный пилот |

## Первый запрос для Codex (TASK-003A)

```text
Работаем в репозитории edward2603-byte/Bitcop.
Базовый коммит a6c07cc87755863e022867a6f87e5a30b7fd451e.
Прочитай AGENTS.md и docs/MODULE_INDEX.md.
В docs/TASK-003_FULL.md прочитай ТОЛЬКО секцию между
BEGIN_TASK_003A и END_TASK_003A, затем раздел «Версия платформы»
в области BEGIN_DETAILED_DESIGN, если он необходим.
Ознакомься с необходимыми исходными файлами, не считывай все модули.
Сначала перечисли затрагиваемые файлы и риски.
Выполни только TASK-003A. Проверь сборку, тесты, LOCAL запуск
и сохранение данных. Подготовь отчёт TASK-003A-RESULT.md.
Не начинай TASK-003B и не меняй сетевую конфигурацию.
```

---

# Часть I. Поэтапные задания Codex

Все пять заданий ниже сохранены по содержанию из исходного пакета. Указания на «большой справочник» относятся к части II этого же документа.


---

<!-- BEGIN_TASK_003A -->

# TASK-003A. Поддерживаемый .NET и воспроизводимая сборка

**База:** `a6c07cc87755863e022867a6f87e5a30b7fd451e`. **Цель:** перевести прототип на поддерживаемый .NET 10 LTS, не меняя бизнес-логики.

## Читать в первую очередь

`AGENTS.md`, `docs/MODULE_INDEX.md`, `README.md`, `src/server/WorkControl.Api/WorkControl.Api.csproj`, `src/agent/WorkControl.Agent/WorkControl.Agent.csproj`, `tests/WorkControl.Api.Tests/WorkControl.Api.Tests.csproj`, `.github/workflows/verify.yml`, `manager.bat`.

По необходимости: раздел «Версия платформы» в части II данного файла («Детальный технический проект»).

## Сделать

1. Перед изменениями проверить, что локальная схема TASK-002 и её тесты работают; зафиксировать результат.
2. Обновить `net8.0` → `net10.0` и `net8.0-windows` → `net10.0-windows`; использовать совместимые **стабильные**, согласованные версии EF Core 10, Npgsql EF 10 и Microsoft.Data.Sqlite 10.
3. Обновить NuGet packages, CI на Windows и инструкции установки SDK/Desktop Runtime.
4. **Не** применять разрушительные миграции; подготовить резервную копию существующей SQLite БД и проверить сохранение старых данных.
5. Запустить restore/build/test для API, агента, web и publish пакета агента. Проверить ручной LOCAL запуск, если доступна Windows GUI-среда.
6. Не трогать устройства реальных сотрудников, сетевой bind, firewall и Identity.

## Приёмка

- `dotnet build` API/агента, `dotnet test`, `npm test/build`, publish агента успешны или явно отмечены как непроверенные с причиной.
- LOCAL: агент → API → БД → браузер не сломан.
- CI зафиксировал поддерживаемые версии.
- Коммит касается только платформы, зависимостей и документации.
- Отчёт `docs/TASK-003A-RESULT.md` включает файл-список, команды, результаты, ограничения.

**STOP после этой задачи.**

<!-- END_TASK_003A -->


---

<!-- BEGIN_TASK_003B -->

# TASK-003B. Вход в панель и права руководителей

**Предусловие:** принята TASK-003A. **Цель:** убрать открытый доступ к отчётам и обеспечить разграничение 3 ролей.

## Читать в первую очередь

`src/server/WorkControl.Api/Program.cs`, `Infrastructure/WorkControlDbContext.cs`, `Domain/Entities.cs`, `Modules/Devices/DeviceService.cs`, `src/web/src/App.tsx`, `src/web/src/api/dashboard.ts`, `docs/MODULE_INDEX.md`.

Из части II данного файла (детальный технический проект) только разделы 3, 5 и 7.

## Сделать

1. Добавить ASP.NET Core Identity (GUID) с `AppUser`, ролями `ADMIN`, `MAIN_USER`, `GROUP_MANAGER`; отдельные Entity `UserGroupAccess` и её FK/индексы.
2. Для SQLite и PostgreSQL создать правильную стратегию EF migrations: сохранить существующие данные; не полагаться на PostgreSQL-only миграцию на SQLite.
3. Bootstrap первого ADMIN только одноразовой локальной CLI-процедурой; никаких записанных в Git паролей или общедоступного `/register`.
4. Cookie auth (`HttpOnly`, `Secure`, `SameSite`, срок жизни), lockout, ограничения попыток, CSRF-защита изменяющих browser-запросов, `logout` и аудит.
5. Реализовать `GET /api/auth/me`, login/logout и защищённый dashboard. В PILOT не выдавать демо-строки при ошибке сети.
6. Ввести серверный query-scope групп: GROUP_MANAGER получает исключительно назначенные группы; при вводе чужого groupId не раскрывать чужие строки.
7. В React создать минимальную страницу входа и сообщение о 401/403. Права не должны контролироваться одним скрытием кнопок.
8. **Не делать** agent enrollment или сетевой доступ.

## Тесты/приёмка

- Anonymous `/api/dashboard` → 401.
- ADMIN видит разрешённый общий отчёт.
- GROUP_MANAGER не получает чужие данные даже при изменении URL/API-параметров.
- Logout закрывает сессию; brute-force ограничен; CSRF у browser writes отклонён.
- Ключи Data Protection сохраняются вне исходников и переживают restart.
- Старая БД обновляется без удаления.

**STOP; составить `TASK-003B-RESULT.md`.**

<!-- END_TASK_003B -->


---

<!-- BEGIN_TASK_003C -->

# TASK-003C. Доверенные агенты: подключение, секреты, отзыв

**Предусловие:** принята TASK-003B. **Цель:** отдельная безопасная идентичность каждого Windows-агента.

## Читать в первую очередь

`src/server/WorkControl.Api/Contracts/AgentContracts.cs`, `Modules/Devices/DeviceService.cs`, `Modules/Telemetry/TelemetryService.cs`, `Infrastructure/WorkControlDbContext.cs`, `src/agent/WorkControl.Agent/AgentEventDispatcher.cs`, `AgentSettings.cs`, `LocalEventStore.cs`.

Из справочника только разделы 4, 5.2–5.3 и 6.

## Сделать

1. Добавить `DeviceEnrollments` (одноразовый код, TTL 10 мин, `CodeHash`, лимиты) и `AgentCredentials` (256-bit random secret, hash only, expire/revoke, KeyId).
2. Admin выпускает одноразовый код. Агент использует **доверенный локальный HTTPS** и ввод кода; устройство создаётся со статусом `Pending`.
3. Администратор в панели одобряет устройство и сам назначает сотрудника/группу. Прекратить автоматическое создание сотрудника по `Environment.UserName`.
4. Агент сохраняет секрет через Windows DPAPI для своего профиля; в `agent-settings.json` оставляет только несекретные настройки.
5. Создать аутентификационную схему `Agent` и endpoint-группу `/api/agent/v1/*` с policy `AgentOnly`. `DeviceId` определяется из проверенных claims, не из присланного AgentId.
6. Старые открытые endpoints регистрации/телеметрии закрыть либо убрать. Не оставлять «временного» открытого обхода.
7. Реализовать статус `Pending/Active/Suspended/Revoked`, безопасный отзыв и ротацию credential.
8. Сохранить SQLite-очередь и её идемпотентность; принимать доставку накопленных за 2 часа событий.
9. Тестировать протокол локально через TLS; **не открывать LAN**.

## Тесты/приёмка

- Нет токена/неверный токен/отозванный токен → 401/403.
- Pending не отправляет рабочую телеметрию.
- Агент A не может отправлять события от имени B.
- Повтор события не создаёт дубль.
- Offline-очередь сохраняется и доставляется после восстановления.
- Регистрационный код одноразовый, просроченный/угаданный отклоняется.
- Логи и ответы API не раскрывают секреты.

**STOP; составить `TASK-003C-RESULT.md`.**

<!-- END_TASK_003C -->


---

<!-- BEGIN_TASK_003D -->

# TASK-003D. HTTPS и ограниченный сетевой пилот

**Предусловие:** принята TASK-003C. **Цель:** разрешить явный тестовый запуск на двух ПК по защищённому каналу.

## Читать в первую очередь

`src/server/WorkControl.Api/Program.cs`, `appsettings.json`, `manager.bat`, `docs/NETWORK_PILOT.md`, `src/agent/WorkControl.Agent/AgentSettings.cs`, `src/web/vite.config.ts`.

Из справочника только разделы 7 и 12.

## Сделать

1. Сохранить LOCAL без автоматического изменения firewall, но не возвращать анонимные endpoints.
2. Добавить отдельный режим `PILOT` с конфигурацией `HTTPS` и доверенным сертификатом, реальным DNS именем и ограничением сетевого сегмента.
3. ИТ-служба вручную предоставляет сертификат; ключи/пароли не хранить в Git. Не отключать TLS verification, не допускать plain HTTP в PILOT.
4. В PILOT React production build обслуживается по тому же origin, что API. Для разработки Vite может оставаться отдельно.
5. Startup validator должен запрещать сетевой старт, если нет TLS, секретов или защищённых key ring.
6. Лимиты запросов, ограничение payload, чистая обработка ошибок (401/403/429) и аудит отказов.
7. Создать отдельную инструкцию включения/остановки, DNS, TLS, firewall и отката. Правила firewall только вручную и для тестового сегмента.
8. Выполнить доступные E2E с сертификатом, затем ручной двухкомпьютерный тест на синтетических данных.

## Приёмка

- С недоверенным сертификатом агент **не подключается**.
- На сетевом интерфейсе нет открытого HTTP endpoint с телеметрией.
- Данные доступны только по подтверждённому HTTPS и разрешённой роли.
- Настоящую сеть / firewall Codex не изменяет самостоятельно.
- Все ограничения и проверенные пункты зафиксированы.

**STOP; составить `TASK-003D-RESULT.md`.**

<!-- END_TASK_003D -->


---

<!-- BEGIN_TASK_003E -->

# TASK-003E. Безопасность, проверка и выпуск ограниченного пилота

**Предусловие:** принята TASK-003D. **Цель:** проверенный и воспроизводимый тест на двух ПК, не массовое внедрение.

## Читать в первую очередь

`docs/NETWORK_PILOT.md`, новые README Identity/Devices/Telemetry/Audit, новые тесты TASK-003B/C/D, `docs/MODULE_INDEX.md`.

Из справочника только разделы 9, 10E, 11, 12.

## Сделать

1. Проверить минимальную матрицу: логин, logout, группы, enrollment, approval, revoke, rotate, offline/online, HTTPS trust, дубль, восстановление после рестарта.
2. Проверить, что аудит не содержит секретов, что Data Protection keys сохраняются и БД восстанавливается из backup.
3. Выполнить валидацию размеров и форматов JSON, временных меток offline-очереди, обработку конфликтов при одновременных событиях.
4. Проверить стабильность при двух агентах на тестовой сети с синтетическими данными.
5. Подготовить понятный `docs/TASK-003-RESULT.md`, инструкцию по установке агентских секретов и списки проверенных/непроверенных условий.
6. Отдельно указать, что не реализовано: постоянный фоновый агент, хронометраж, полнофункциональная структура дня, программы, продуктивность, объяснения отсутствия.
7. Не расширять пилот без владельца проекта и внутренних согласований.

## Приёмка

- Доступы проверены с двумя ролями и разными группами.
- Отзыв устройства и прав пользователя фактически проверен.
- Сервер корректно перезапускается с сохранением ключей, записей, логов.
- Имеется ручная stop/rollback процедура.
- Нет критических неизвестных для пилота; остаточные риски перечислены.

**STOP после отчёта.**

<!-- END_TASK_003E -->


---

# Часть II. Полный детальный технический проект (с примерами C#)

Это полный текст технического проекта. Читайте его **выборочно по номеру раздела**, а не целиком при каждом вызове Codex.

<!-- BEGIN_DETAILED_DESIGN -->


# WorkControl — TASK-003: детальный технический проект для Codex

**База анализа:** `edward2603-byte/Bitcop`, коммит `a6c07cc87755863e022867a6f87e5a30b7fd451e` (24.11.2026).  
**Назначение:** развитие работающего локального прототипа до защищённого *тестового* сетевого пилота.  
**Метод:** последовательные TASK-003A → 003B → 003C → 003D → 003E, каждая заканчивается работающей сборкой и тестами.  
**Статус документа:** технический дизайн и исходное задание. Примеры кода являются проектными фрагментами, а не готовым проверенным production-решением.

## 0. Текущее состояние и ограничения

**Подтверждено по исходникам коммита:**

- ASP.NET Core API и EF Core `WorkControlDbContext`, SQLite в локальном запуске и предусмотренный PostgreSQL;
- `POST /api/devices/register`, `POST /api/telemetry/events`, `GET /api/dashboard` без аутентификации;
- устройство идентифицируется клиентским `AgentId`, при регистрации сервер может создать сотрудника по имени Windows;
- WPF-агент хранит `agent-settings.json` и локальную SQLite-очередь; отправляет состояние `active`/`idle` раз в 30 секунд;
- на текущий момент агент принимает только loopback HTTP, поэтому сетевой пилот недоступен;
- React-панель через Vite proxy использует `/api/dashboard`; встроенного входа нет;
- в `Program.cs` сводка загружает **все** события каждого устройства; это отдельно улучшать на этапе отчётов.

**Не подтверждено:** фактическая работа всех функций на нескольких компьютерах, нагрузка в 100 агентов, Windows-доменная интеграция, поведение за реверс-прокси. Нельзя считать их реализованными.

**Критические исходные условия:** один офис; около 100 компьютеров; Windows 10/11; иерархия `организация → отдел → группа → сотрудник`; пользователи ADMIN, MAIN_USER, GROUP_MANAGER. Данные о сотрудниках нельзя раскрывать между группами. Система предназначена для информированного корпоративного мониторинга; не внедрять скрытые механизмы наблюдения.

### Версия платформы

Текущий проект использует `net8.0`/`net8.0-windows` и NuGet-пакеты 8.x. Поддержка .NET 8 закончилась 10.11.2026. В **отдельной первой задаче** проверить переход на поддерживаемую ветку **.NET 10 LTS** (`net10.0`/`net10.0-windows`), EF Core 10.x и совместимые стабильные версии Npgsql 10.x; не смешивать этот переход с добавлением авторизации.

Проверить Windows 10/11, сборку WPF, GitHub Actions, пакет агента, создание SQLite и PostgreSQL миграции. Если миграция не проходит, остановить первую задачу с точной диагностикой, не проводить молчаливый откат функциональности. Версии пакетов зафиксировать в зависимостях (не использовать `latest`).

## 1. Целевая архитектура

```text
                       Корпоративный LAN / VPN (PILOT)

 Windows 10/11, агент                         Сервер организации
 ┌─────────────────────────────┐             ┌───────────────────────────────────┐
 │ Agent.WPF (пользователь)    │  HTTPS      │ ASP.NET Core (единый процесс)    │
 │ Agent.Core (события)        ├────────────►│ /api/agent/*  auth: AGENT       │
 │ Agent.Transport             │             │ /api/admin/*  auth: COOKIE      │
 │ Agent.Storage SQLite        │             │ /api/reports/* auth: COOKIE     │
 │ Agent.Security (DPAPI)      │             │ Identity / Devices / Telemetry  │
 └─────────────────────────────┘             │ Organization / Audit            │
                                           │ EF Core / PostgreSQL            │
 Управляющий браузер                         │ React: same-origin static build   │
 ┌─────────────────────────────┐  HTTPS      └───────────────────────────────────┘
 │ React + HttpOnly cookie     ├────────────►
 │ CSRF token on writes        │
 └─────────────────────────────┘

 LOCAL: одна машина, loopback-only; режим не должен исчезнуть.
 PILOT: отдельная явная конфигурация, TLS и сетевые правила только после проверки.
```

**Модульный монолит:** не создавать отдельные разворачиваемые микросервисы. Контракты агента и веб-панели версионировать. Авторизация браузера и аутентификация агента — **разные схемы** и разные права. Нельзя использовать один общий «админский» токен для всех ПК.

## 2. Целевая структура файлов

```text
src/server/WorkControl.Api/
  Program.cs                       # только DI, middleware, регистрация endpoint-групп
  Contracts/
    AgentContracts.cs              # версия v1, только безопасные DTO
    IdentityContracts.cs
    EnrollmentContracts.cs
  Infrastructure/
    WorkControlDbContext.cs
    Configurations/                # EF mapping по модулям
    MigrationsSqlite/              # отдельный набор миграций SQLite (если необходим)
    MigrationsPostgres/            # отдельный набор миграций PostgreSQL
  Modules/
    Identity/
      AppUser.cs                   # ASP.NET Core Identity GUID
      IdentityEndpoints.cs
      AuthPolicies.cs
      GroupAccessService.cs
      README.md
    Devices/
      DeviceService.cs             # НЕ принимает доверенные сведения о сотруднике от агента
      EnrollmentService.cs
      AgentCredentialService.cs
      AgentAuthenticationHandler.cs
      DeviceEndpoints.cs
      Models/                     # Enrollment, Credential, DeviceStatus
      README.md
    Telemetry/
      TelemetryService.cs
      TelemetryEndpoints.cs
      README.md
    Organization/
      GroupManagementService.cs
      README.md
    Audit/
      AuditEvent.cs
      AuditService.cs
      README.md
  Security/
    SecuritySettings.cs
    StartupSecurityValidator.cs
    DataProtectionConfiguration.cs

src/agent/WorkControl.Agent/
  AgentSettings.cs
  AgentEventDispatcher.cs
  LocalEventStore.cs
  Security/
    CredentialStore.cs           # Windows DPAPI, per-user
    AgentEnrollmentClient.cs
    TrustedServerConfiguration.cs

src/web/src/features/
  auth/                           # вход, выход, /api/auth/me, обработка 401/403
  administration/devices/         # заявка на устройство, разрешение/отзыв
  administration/users/           # пользователи/роли
  administration/groups/          # доступные группы

src/web/src/api/
  auth.ts
  agent-admin.ts
  dashboard.ts

tests/
  WorkControl.Api.Tests/
    IdentityAccessTests.cs
    EnrollmentTests.cs
    AgentAuthenticationTests.cs
    TelemetrySecurityTests.cs
    CsrfTests.cs

  WorkControl.Agent.Tests/
    CredentialStoreTests.cs
    AgentEnrollFlowTests.cs

ops/
  pilot/                          # сертификаты ИТ, примеры конфигурации, НЕТ секретов
  local/                          # LOCAL сценарий

docs/
  SECURITY_ARCHITECTURE.md
  API_CONTRACTS.md
  FIRST_RUN_CHECKLIST.md
  NETWORK_PILOT.md
  TASK-003A.md ... TASK-003E.md
```

**Не создавать все папки сразу.** Добавлять только файлы соответствующей подзадачи. Не переносить все прежние модели в новый `Shared` и не устраивать массовый рефакторинг «ради красоты». `Program.cs` должен уменьшаться по мере выделения endpoint-групп.

## 3. Модель пользователей и доступа

### 3.1. Разделять сотрудника и пользователя панели

- `Employee` — контролируемый сотрудник, привязанный к группе.
- `AppUser` — учётная запись в панели, может быть администратором/главным пользователем/руководителем группы; необязательно совпадает с контролируемым `Employee`.
- `WorkGroup` — группа в существующей модели.
- `UserGroupAccess` — выдача прав доступа к одной конкретной группе пользователю панели.
- `Device` — физический/логический компьютер, отдельно привязываемый к сотруднику администратором.

**Роли:** `ADMIN`, `MAIN_USER`, `GROUP_MANAGER`.

| Действие | ADMIN | MAIN_USER | GROUP_MANAGER |
|---|---:|---:|---:|
| Просмотр собственных доступных групп | Да, все | Да, все | Только назначенные |
| Просмотр всей организации | Да | Да | Нет |
| Назначение компьютеров сотрудникам | Да | Настраиваемо | Нет |
| Регистрация и отзыв агентов | Да | Настраиваемо | Нет |
| Изменение ролей и учётных записей | Да | Нет по умолчанию | Нет |
| Изменение группового доступа | Да | Нет по умолчанию | Нет |
| Просмотр журнала безопасности | Да | Настраиваемо | Нет |

Не полагаться только на `[Authorize(Roles=...)]`: ограничение **по группе** проверяется отдельно *в базе и в запросах*.

### 3.2. Рекомендуемая реализация

- ASP.NET Core Identity: проверенные механизмы хранения паролей, lockout, безопасные cookies, роли.
- Логин руководителя через HttpOnly cookie, `Secure` и `SameSite=Lax` или `Strict` по согласованной схеме; срок сессии и выход.
- Защита от CSRF для всех изменяющих состояние браузерных запросов, проверка Origin/Host, ограничение частоты попыток входа.
- **Не подключать открытый `MapIdentityApi` с публичной самостоятельной регистрацией**, если регистрация не закрыта; пользователей создаёт только администратор.
- Создание первоначального администратора отдельной одноразовой локальной командой, без пароля в git, без «admin/admin» и без постоянного bootstrap-endpoint.
- В пилоте React собран и обслуживается по **тому же origin**, что и API, чтобы избежать необязательных междоменных cookies/CORS.
- Хранить ASP.NET Core Data Protection key ring устойчиво, с ограниченным доступом, чтобы после перезапуска пользователи не теряли сессии.
- Публичные интерфейсы администрирования проверять и серверными тестами, и тестами попыток доступа с чужой группой.

### 3.3. EF-модель, пример (эскиз)

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public sealed class AppUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}

public sealed class UserGroupAccess
{
    public Guid UserId { get; set; }
    public Guid GroupId { get; set; }
}

// Пример целевого перехода; существующие DbSet и связи СОХРАНИТЬ.
public sealed class WorkControlDbContext
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public WorkControlDbContext(DbContextOptions<WorkControlDbContext> options)
        : base(options) { }

    public DbSet<UserGroupAccess> UserGroupAccesses => Set<UserGroupAccess>();
    // Organizations, WorkGroups, Employees, Devices, AgentEvents ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // обязательно для ASP.NET Core Identity
        modelBuilder.Entity<UserGroupAccess>()
            .HasKey(x => new { x.UserId, x.GroupId });
        // Перенести имеющиеся EF-конфигурации без изменения смысла данных.
    }
}
```

**Внимание к миграциям:** в исходном проекте SQLite использует `EnsureCreated`, PostgreSQL — миграцию с PostgreSQL-типами (`uuid`, `jsonb`). Нельзя считать одну миграцию переносимой. Перед изменением модели зафиксировать стратегию миграций для обоих провайдеров, сделать резервную копию локальной БД, протестировать обновление базы с существующими данными и чистую установку. `EnsureCreated` для рабочего пилота заменить миграциями, но **не удалять базы и не применять `EnsureDeleted` как способ исправления ошибки**.

## 4. Аутентификация компьютеров

### 4.1. Угроза в текущем коде

Сейчас `POST /api/devices/register` доверяет строке `agentId`, а `POST /api/telemetry/events` принимает `agentId` без подтверждения владения. Злоумышленник с доступом к API мог бы выдавать себя за другой компьютер. Сведения о Windows-пользователе также приходят от клиента и не являются удостоверением личности.

### 4.2. Рекомендуемая схема для пилота

**Одно устройство = индивидуальная учётная запись агента = индивидуальный отзывный секрет.**

Жизненный цикл:

1. Администратор в панели инициирует подключение устройства и получает **одноразовый случайный код регистрации** с коротким сроком действия (например, 10 минут). Код показывается один раз.
2. На пилотном компьютере пользователь вводит адрес HTTPS сервера и регистрационный код.
3. Агент отправляет код + технические сведения о ПК по защищённому каналу.
4. Сервер проверяет TTL, одноразовость, частотные ограничения, создаёт `Device` в состоянии `Pending` и выдаёт отдельный сильный секрет агента. Код регистрации сразу погашается.
5. Агент сохраняет секрет **зашифрованным средствами Windows**. Сервер хранит только криптографический хеш секрета; в логах и интерфейсе полный секрет никогда не показывается.
6. Администратор видит заявку, проверяет имя/инвентарный номер компьютера и переводит устройство в состояние `Active`, назначая сотрудника и группу. До утверждения агент не может присылать рабочую телеметрию.
7. Каждая отправка телеметрии аутентифицируется индивидуальным секретом; сервер определяет `DeviceId` **из аутентифицированного контекста**, не из клиента.
8. Администратор может приостановить устройство, отозвать секрет или переиздать новый; старая пара ключей немедленно становится недействительной.

**Токены:** минимум 256 бит криптостойкой случайности; хранить на сервере хеш (`SHA-256` допустим именно для высокоэнтропийного случайного секрета, но не для человеческого пароля). Использовать идентификатор секрета (`KeyId`) для поиска, затем сравнивать хеш без зависимости времени от значения. В журналах никогда не логировать Authorization header.

При потере связи (включая два часа offline) события остаются в локальной очереди и после восстановления отправляются с прежним `EventId`. Клиентские времена допускаются как исторические, но валидируются на разумный горизонт и отмечаются отдельно при расхождении часов. **Не требовать, чтобы каждое событие offline имело timestamp менее 5 минут**, иначе двухчасовая очередь не заработает.

### 4.3. Сущности данных

| Таблица | Поля (минимум) | Особенности |
|---|---|---|
| `Devices` | `Id`, `AgentId`, `ComputerName`, `EmployeeId?`, `Status`, `EnrolledAtUtc`, `ApprovedAtUtc?`, `LastSeenAtUtc?` | `EmployeeId` должен стать nullable для заявки до назначения |
| `DeviceEnrollments` | `Id`, `CodeHash`, `ExpiresAtUtc`, `UsedAtUtc?`, `CreatedByUserId`, `AttemptCount` | TTL, one-use, блокировка перебора |
| `AgentCredentials` | `Id/KeyId`, `DeviceId`, `SecretHash`, `CreatedAtUtc`, `ExpiresAtUtc?`, `RevokedAtUtc?` | Хеш на сервере, ротация и отзыв |
| `UserGroupAccess` | `UserId`, `GroupId` | Составной первичный ключ, FK |
| `AuditEntries` | `Id`, `ActorType`, `ActorId`, `Action`, `TargetType`, `TargetId`, `AtUtc`, `Result` | Без секретов и содержимого телеметрии |

Защитить от дублей и гонок на уровне уникальных индексов/транзакций. Устройства не могут автоматически переназначать себя с одного сотрудника на другого. История должна сохранять прежнюю привязку на момент события либо явную версионированную историю назначений.

### 4.4. Минимальный криптографический фрагмент C# (эскиз)

```csharp
using System.Security.Cryptography;

public static class DeviceSecrets
{
    // Сервер должен показать секрет ровно один раз при безопасной выдаче.
    public static byte[] Generate() => RandomNumberGenerator.GetBytes(32);

    public static byte[] Hash(ReadOnlySpan<byte> secret) => SHA256.HashData(secret);

    public static bool Matches(ReadOnlySpan<byte> providedSecret,
                               ReadOnlySpan<byte> storedHash)
    {
        var actual = SHA256.HashData(providedSecret);
        return CryptographicOperations.FixedTimeEquals(actual, storedHash);
    }
}
```

Этот фрагмент **не** решает весь протокол: нужны схема форматирования токена, поиск `KeyId`, expiry/revocation, транзакции и тесты. Не вставлять секрет в URL; использовать заголовок Authorization только через HTTPS.

### 4.5. Защита секрета Windows

Текущий `agent-settings.json` может содержать адрес API и несекретные настройки, но **не секрет агента в открытом виде**. Хранить секрет отдельно через DPAPI `CurrentUser` и ограничение ACL; для будущего фонового агента определить, под какой учётной записью он будет выполняться, чтобы секрет оставался доступным нужному Windows-сеансу. Не смешивать `CurrentUser` с `LocalMachine` без явного решения.

```csharp
// ЭСКИЗ механизма защиты байтов на Windows;
// добавить подходящую стабильную версию System.Security.Cryptography.ProtectedData.
using System.Security.Cryptography;

byte[] encrypted = ProtectedData.Protect(
    rawTokenBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
byte[] decrypted = ProtectedData.Unprotect(
    encrypted, optionalEntropy: null, DataProtectionScope.CurrentUser);
```

Проверить права файловой системы и сценарий смены Windows-профиля. Не логировать расшифрованное значение.

## 5. Разделение схем доступа в ASP.NET Core

**Схема A, `Identity.Application`:** браузерные users (cookie).  
**Схема B, `Agent`:** только агенты (индивидуальный credential).  
Нельзя принимать cookie как эквивалент разрешения агента, а агентским токеном открывать администраторские страницы.

### 5.1. Схематичный DI / политики (до компиляции интегрировать с реальным проектом)

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

builder.Services
    .AddIdentity<AppUser, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<WorkControlDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "WorkControl.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, AgentAuthenticationHandler>(
        "Agent", _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AgentOnly", new AuthorizationPolicyBuilder("Agent")
        .RequireAuthenticatedUser().Build());
    options.AddPolicy("AllManagers", new AuthorizationPolicyBuilder(
            IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser()
        .RequireRole("ADMIN", "MAIN_USER", "GROUP_MANAGER")
        .Build());
    options.AddPolicy("AdminOnly", new AuthorizationPolicyBuilder(
            IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser().RequireRole("ADMIN").Build());
});

// Порядок: TLS/host security -> authentication -> authorization -> endpoints.
app.UseAuthentication();
app.UseAuthorization();
```

`AgentAuthenticationHandler` — отдельный класс, который получает заголовок, проверяет ключ, статус устройства, срок действия и возвращает claims `device_id`, `credential_id` со схемой `Agent`. Использовать официальную абстракцию ASP.NET Core `AuthenticationHandler<TOptions>` и актуальные API .NET 10. Не делать проверки вручную в каждом endpoint.

**Важно:** политика `AllManagers` подтверждает только роль; доступ к конкретным группам должен применять `IGroupAccessService` к **запросу данных**. Если параметры отчёта содержат произвольный `groupId`, сервер обязан проверить его и вернуть 403 либо ограничить выборкой разрешённых групп. Не выдавать данные, а затем фильтровать их в React.

### 5.2. Пример защищённого endpoint телеметрии

```csharp
var agents = app.MapGroup("/api/agent/v1")
    .RequireAuthorization("AgentOnly");

agents.MapPost("/events", async (
    HttpContext context,
    AgentEventV1 request,
    ITelemetryIngestionService service,
    CancellationToken ct) =>
{
    var deviceClaim = context.User.FindFirst("device_id")?.Value;
    if (!Guid.TryParse(deviceClaim, out var deviceId))
        return Results.Unauthorized();

    // DeviceId — только из сервером проверенного контекста.
    var result = await service.AcceptAsync(deviceId, request, ct);
    return Results.Ok(result);
});
```

`AgentEventV1` не должен принимать используемый для авторизации `AgentId` как основание для выбора сотрудника. Даже если поле оставлено ради старого контракта, сравнивать его с подтверждённым устройством и никогда не использовать как доверенное удостоверение.

### 5.3. Пример фильтрации данных по группам

```csharp
public IQueryable<Employee> ApplyEmployeeScope(
    IQueryable<Employee> source,
    Guid currentUserId,
    bool canSeeAllGroups,
    WorkControlDbContext db)
{
    if (canSeeAllGroups) return source;
    return source.Where(employee =>
        db.UserGroupAccesses.Any(access =>
            access.UserId == currentUserId &&
            access.GroupId == employee.WorkGroupId));
}
```

Код демонстрирует идею: **ограничивать IQueryable до материализации**. Для серверных отчётов выбирать только разрешённых сотрудников/устройства. Само право `canSeeAllGroups` определяется сервером по актуальным ролям, не аргументом клиента. Проверять, что ранее выданная cookie не сохраняет отозванные групповые права навсегда.

## 6. Маршруты API (предлагаемые)

| Маршрут | Схема | Результат |
|---|---|---|
| `POST /api/auth/login` | публичный, throttled + CSRF/origin | Вход руководителя, HttpOnly cookie |
| `POST /api/auth/logout` | BrowserCookie + CSRF | Завершить сессию |
| `GET /api/auth/me` | BrowserCookie | Роль, доступные группы, возможности |
| `GET /api/reports/dashboard` | `AllManagers` + group scope | Сводка с ограничением групп |
| `POST /api/admin/enrollments` | `AdminOnly` + CSRF | Сформировать код приглашения |
| `POST /api/agent/v1/enroll` | публичный HTTPS, код/лимиты | Создать Pending-устройство, выдать credential |
| `POST /api/admin/devices/{id}/approve` | `AdminOnly` + CSRF | Разрешить устройство |
| `POST /api/admin/devices/{id}/revoke` | `AdminOnly` + CSRF | Отозвать доступ |
| `POST /api/admin/devices/{id}/rotate` | `AdminOnly` + CSRF, специальный безопасный протокол доставки | Начать ротацию credential |
| `POST /api/agent/v1/events` | `AgentOnly` | События устройства |
| `GET /api/agent/v1/config` | `AgentOnly` | Настройки конкретного агента |
| `GET /health/live` | локальный/минимальные сведения | Проверка процесса без данных организации |

**Не оставлять старые открытые** `POST /api/devices/register`, `POST /api/telemetry/events`, `GET /api/dashboard`. Перевести их на новые защищённые контракты или удалить после обновления агента/React. Не оставлять альтернативный незащищённый маршрут «для совместимости». Проверить статус-коды 400/401/403/409 и повторную доставку.

### 6.1. DTO версии v1 (эскиз)

```csharp
public sealed record AgentEnrollmentRequestV1(
    string EnrollmentCode,
    string ComputerName,
    string AgentInstanceId,
    string AgentVersion);

public sealed record AgentEnrollmentResponseV1(
    Guid DeviceId,
    string CredentialId,
    string CredentialSecret, // Однократно, только HTTPS!
    string Status);

public sealed record AgentEventV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string EventType,
    string? PayloadJson);

public sealed record DeviceStateV1(
    Guid DeviceId,
    string Status,
    DateTimeOffset? LastSeenAtUtc);
```

Чётко ограничить максимальный размер `PayloadJson`, валидировать `EventType`, не доверять `ComputerName` для разрешений. `CredentialSecret` не возвращать в журналах, дампах ошибок и административных API. Статусы устройств: `Pending`, `Active`, `Suspended`, `Revoked`.

## 7. HTTPS, локальный и сетевой режимы

### 7.1. LOCAL

- Сохраняется возможность запуска на одном Windows-компьютере.
- Открытые endpoints уже после TASK-003 должны быть защищены и локально, **не создавать постоянный обход авторизации**.
- Разрешить localhost HTTP **только** как изолированный промежуточный dev-режим до HTTPS, с явным development-флагом, недоступным в PILOT/Production. Желательно перевести и LOCAL на доверенный dev-сертификат.
- Пользовательский `manager.bat` не открывает LAN-порт и не добавляет firewall rule.

### 7.2. PILOT

- Один адрес: `https://workcontrol-pilot.company.lan` (условный DNS; заменить реальным через администраторов сети).
- Сервер и панель — один HTTPS origin; React production build должен обслуживаться сервером, а не оставлять Vite-dev-сервер как основную точку доступа.
- Сертификат с SAN на используемое DNS-имя, доверенный всеми тестовыми ПК; выпуск/установка через ИТ-службу. Не генерировать и не публиковать приватный ключ в репозитории.
- Проверка сертификата агентом включена; запрет `DangerousAcceptAnyServerCertificateValidator`, `ServerCertificateCustomValidationCallback = true` и HTTP в PILOT.
- Конфигурация DNS/port/certificate paths через защищённые параметры запуска, без токенов в JSON в Git.
- Явная команда/скрипт для сетевого режима, запуск только уполномоченным администратором и с проверкой prerequisites.
- Firewall: только определённые тестовые IP/сегмент, не `0.0.0.0` в произвольном режиме, и не автоматическое добавление всех правил при старте.
- Секреты и Data Protection key ring переживают рестарт, архивируются по правилам организации.

### 7.3. CSRF и клиентские ошибки

Браузерные `POST/PUT/PATCH/DELETE` с cookie требуют CSRF-защиты. Для SPA реализовать согласованную схему получения и передачи anti-forgery token, не ограничиваться атрибутом `SameSite`. CORS не открывать `*` для cookie. Для агента с header credential отдельная CSRF-проверка не нужна, но обязательны auth, TLS, лимиты и проверка payload.

React должен различать:
- `401`: предложить вход/обновление сессии;
- `403`: нет разрешения на выбранную группу/действие;
- `429`: слишком много запросов;
- `503`/offline: сервер недоступен, не показывать выдуманную рабочую статистику.

Демо-данные не должны появляться на защищённых бизнес-страницах в PILOT даже при ошибке сети.

## 8. Фоновая работа агента: граница TASK-003

В TASK-003 **не** нужно полноценно переписывать агент в службу Windows. Его текущая работа в WPF и сбор через `GetLastInputInfo` связаны с пользовательским сеансом. Служба Session 0 не получает доступ к активности интерактивного рабочего стола автоматически.

На этом этапе:
- вынести transport/enrollment/credential-store в отдельные классы;
- сохранить работающий WPF интерфейс для регистрации и диагностики;
- предусмотреть будущий `Agent.Core`/`Agent.SessionWorker` и запуск при входе пользователя;
- не ломать текущую локальную очередь;
- гарантировать, что секреты привязаны к правильному Windows-профилю.

Полная автономная постоянная работа агента — отдельная TASK-004 после защищённого сетевого пилота.

## 9. Аудит и защита данных

Обязательные события: `login_success`, `login_failure` (без пароля), `logout`, `user_created`, `role_changed`, `group_access_changed`, `enrollment_issued`, `enrollment_consumed`, `device_approved`, `device_revoked`, `credential_rotated`, `agent_auth_failed`, `pilot_enabled`, `pilot_disabled`.

В аудите хранить кто, когда, тип действия, цель, результат, технический correlation ID; не хранить сам секрет, cookies, полные заголовки Authorization, тексты рабочих документов и содержимое экранов.

Отдельные параметры: сроки хранения аудита, технической телеметрии, отключение/удаление профилей, архив. Настройки не угадывать: до корпоративного согласования использовать синтетические данные. Настройка журнала не должна превращаться в сбор содержимого клавиатуры.

## 10. Итерации для Codex: точный порядок

### TASK-003A — обновить платформу и привести инфраструктуру в порядок

**Затронуть:** `.csproj`, GitHub Actions, `manager.bat`, тесты, `docs/DEVELOPMENT.md`; миграции только если необходимо для совместимости.

1. Перевести API и WPF на .NET 10 LTS и согласованные стабильные версии пакетов (EF 10/Npgsql 10). Сохранить SQLite для локального запуска.
2. Проверить `dotnet restore/build/test`, `npm ci/test/build`, `dotnet publish`.
3. Прогнать чистый запуск и запуск на базе, созданной предыдущей версией; сделать backup перед структурными преобразованиями.
4. Проверить работоспособность WPF на целевых Windows 10/11.
5. Убрать неявные `latest` из runtime-зависимостей, зафиксировать версии.
6. Задокументировать изменения; **STOP**. Не добавлять Identity в этой итерации.

**Приёмка:** локальный агент → API → таблица работает как до обновления; тесты зелёные; CI создан/проверен; при невозможности обновления указана причина и план.

### TASK-003B — логин руководителя + ограничение групп

**Затронуть:** `Modules/Identity`, модель AppUser, DbContext+миграции, `GroupAccessService`, React `auth`, защищённый dashboard, тесты.

1. Встроить ASP.NET Core Identity GUID со своим `AppUser`, ролями и AppUserGroupAccess.
2. Одноразовый bootstrap ADMIN **локальной CLI-командой**, без публичной регистрации.
3. Cookie auth, lockout, анти-CSRF для браузерных POST, безопасные session cookies, аудит входа.
4. Защитить `/api/dashboard` и все административные endpoints.
5. Реализовать серверный group-scope: GROUP_MANAGER получает данные только назначенных групп, даже если подставит чужой groupId.
6. React login/logout/me и корректные 401/403; в PILOT не использовать демо-fallback.
7. Сохранить локальный запуск; тесты нескольких пользователей.
8. **STOP**: агентская аутентификация ещё отдельно.

**Приёмка:** без логина `/api/dashboard` → 401; руководитель чужой группы → 403 либо пустая разрешённая выборка; ADMIN видит все; пароль не хранится открытым; logout реально прекращает сессию.

### TASK-003C — учётные данные агента + регистрация/одобрение

**Затронуть:** `Devices`, `AgentAuthenticationHandler`, `Telemetry` endpoints, агент `Transport/Security`, DbContext, тесты.

1. Реализовать `DeviceEnrollments` и `AgentCredentials` с ограниченным TTL/one-use/отзывом.
2. Администратор вручную выдаёт короткоживущий код; агент использует HTTPS, получает уникальный credential; статус Pending.
3. Администратор утверждает устройство и привязывает сотрудника/группу; самостоятельное создание сотрудника агентом отключить.
4. Все события принимаются только по Agent-auth, DeviceId берётся из claims; старые открытые endpoints закрыть.
5. Секрет хранится защищённо через Windows DPAPI, локальная очередь остаётся работоспособной.
6. Регистрация, проверка права, дедупликация, отзыв и ротация покрыты тестами.
7. **STOP**: не открывать LAN до TASK-003D.

**Приёмка:** неподключённый/отозванный агент → 401/403; подмена AgentId не даёт доступ к другому устройству; офлайн-очередь отправляется после восстановления; чужой сотрудник не появляется автоматически.

### TASK-003D — HTTPS и ограниченный сетевой PILOT

**Затронуть:** Kestrel/reverse proxy, серверная конфигурация, agent trusted endpoint, React static serving, скрипты, docs.

1. Настроить единый HTTPS origin с доверенным корпоративным сертификатом.
2. Реализовать защищённую конфигурацию двух режимов LOCAL/PILOT и запрет старта PILOT без TLS/секретов/ключей.
3. Включить server-side startup validation, rate limits, security headers, запрет HTTP на сетевом интерфейсе.
4. Подготовить явную инструкцию ИТ-администратору для DNS, сертификата, firewall с ограниченным диапазоном.
5. E2E-проверка на **двух тестовых ПК** и с синтетическими данными; ручные проверки подлежат подтверждению пользователем.
6. **STOP**. Не подсоединять автоматически 100 компьютеров.

**Приёмка:** клиент с недоверенным сертификатом не подключается; без credentials телеметрия не проходит; админ просматривает разрешённый тестовый ПК; отключение/отзыв работает.

### TASK-003E — укрепление, измерения и выпуск пилота

**Затронуть:** тесты, метрики, docs, минимальные исправления обнаруженных проблем.

1. Проверить сценарии с двумя пользователями, двумя группами, двумя компьютерами, offline/online, restart, revoked/rotated credentials.
2. Проверить логирование без секретов, восстановление ключей Data Protection, резервную копию и восстановление БД.
3. Проверить отрицательные тесты, лимиты размера payload, ошибочные времена, повторную отправку.
4. Оформить протокол результатов, известные ограничения, список разрешённых машин, stop/rollback инструкцию.
5. Перечислить, какие дальнейшие функции ещё отсутствуют: фоновые агенты, отчёты, программы, продуктивность, скриншоты, причины простоя.
6. **STOP**. Запросить отдельное решение о расширении тестового пилота.

**Приёмка:** подтверждена безопасность и воспроизводимость двухкомпьютерного пилота. Никаких заявлений «готово для 100 ПК» без нагрузочного тестирования.

## 11. Тестовая матрица (не пропускать)

| ID | Сценарий | Ожидаемый результат |
|---|---|---|
| S01 | GET `/api/dashboard` без cookie | 401, нет персональных данных |
| S02 | Неверный пароль | Отказ, учёт попыток, throttling |
| S03 | Logout | Cookie инвалидируется; доступ повторно требует вход |
| S04 | GROUP_MANAGER запрашивает свою группу | Разрешённая выборка |
| S05 | GROUP_MANAGER подставляет чужой GroupId | 403 или пустая выборка, никаких чужих данных |
| S06 | ADMIN запрашивает группы | По назначенным правам полный доступ |
| S07 | POST без CSRF-токена с browser-cookie | 400/403 |
| S08 | Повторное использование enrollment-кода | Отказ |
| S09 | Просроченный enrollment-код | Отказ |
| S10 | Массовый перебор кода | Ограничение частоты и аудит |
| S11 | Агент без credential | 401 |
| S12 | Агент с неверным credential | 401 |
| S13 | Агент A пытается передать DeviceId B | Событие остаётся привязанным к A или отказ |
| S14 | Агент со статусом Pending | Нет приёма телеметрии |
| S15 | Агент после одобрения | Приём, привязка к утверждённому ПК |
| S16 | Агент после отзыва | Отказ в приёме |
| S17 | Ротация credential | Старый перестаёт действовать после контролируемого перехода |
| S18 | Повтор одного EventId | Одна запись, duplicate=true |
| S19 | 2 часа offline | События в буфере, затем корректная доставка |
| S20 | Недоверенный TLS-сертификат | Подключение агента не устанавливается |
| S21 | HTTPS PILOT, запрос по HTTP | Нет незашифрованного обмена рабочими данными |
| S22 | Перезапуск API | Ключи Identity/Data Protection и данные сохраняются |
| S23 | Сервер не работает | Агент не теряет локальную очередь |
| S24 | Отключили пользователя панели | Доступ закрывается по заданной политике |
| S25 | Старый legacy `/api/telemetry/events` | Не остаётся анонимным каналом |
| S26 | Проверка audit | Действие записано, секретов в журнале нет |
| S27 | Неправильный JSON/большой payload | 400/413 и отсутствие повреждения данных |
| S28 | CI на Windows | API/агент/web и тесты собираются |

## 12. Порядок ручного испытания после TASK-003E

1. ИТ-администратор заранее согласовал тестовую сеть, DNS и сертификат; пользователи тестовых устройств информированы.
2. На сервере создаётся администратор. Проверяется вход без предустановленного пароля.
3. Создаются две разные группы, по одному синтетическому сотруднику на группу.
4. Создаются ADMIN и GROUP_MANAGER; GROUP_MANAGER получает доступ только к одной группе.
5. На двух тестовых Windows-компьютерах запускаются агенты и вводятся одноразовые коды.
6. В панели администратор видит Pending-заявки, проверяет устройства и разрешает их.
7. Убедиться, что события поступают; ограничить группу руководителя и проверить утечки.
8. На одном ПК отключить сеть на несколько минут, затем включить и проверить очередь.
9. Отозвать один агент; новые события от него перестают приниматься.
10. Остановить сетевой пилот по documented stop-процедуре.

**До этого** допустим только текущий LOCAL-режим и чисто синтетические тесты; не ставить программу массово на сотрудников.

## 13. Правила экономии токенов Codex

- Каждый запуск получает **ровно одну секцию** TASK-003X этого единого документа (см. явные маркеры `BEGIN_TASK_003X` / `END_TASK_003X`).
- Прежде чем писать код: прочесть `AGENTS.md`, `docs/MODULE_INDEX.md`, нужные **3–8 файлов**, а не всё дерево.
- Сначала короткий план изменения и список затрагиваемых модулей; потом код, сборка, тесты.
- Не затрагивать M06 (расчёты времени), Productivity, Screenshots, BrowserActivity, модуль причин отсутствия.
- Изменение EF-схемы должно сопровождаться миграциями, тестом чистой БД и тестом обновления БД.
- Запрещены «временные» открытые админские endpoints, заглушки аутентификации и автоматическое изменение прав брандмауэра.
- После задачи обновить только `README` соответствующих модулей, `MODULE_INDEX`, `DECISIONS` и текущий TASK-лог.
- Не выводить в ответ десятки файлов целиком: список файлов, сводка диффа, результаты тестов, остаточные риски.
- Не переходить к следующей задаче автоматически.

## 14. Точный стартовый запрос к Codex (вставить один раз)

> Репозиторий `edward2603-byte/Bitcop`, база `a6c07cc87755863e022867a6f87e5a30b7fd451e`. Прочитай `AGENTS.md`, `docs/MODULE_INDEX.md`, секцию `TASK-003A` файла `docs/TASK-003_FULL.md` и только нужные разделы его технического проекта, затем только исходники, относящиеся к TASK-003A. Начни с **TASK-003A: переход на поддерживаемую .NET 10 LTS и проверка текущей системы**. Не реализуй TASK-003B/C/D/E и не изменяй сетевые настройки. До изменений перечисли затрагиваемые файлы, зависимости и риски миграции SQLite/PostgreSQL. Сохрани рабочую схему LOCAL. После изменений выполни доступные сборки и тесты и представь отчёт с точными командами. Если чего-то не удалось проверить, напиши «не проверено». Заверши работу после TASK-003A.

**После завершения TASK-003A** отдельный запрос: «Прочитай в `docs/TASK-003_FULL.md` только секцию TASK-003B и необходимые разделы технического проекта. Реализуй TASK-003B; ничего из 003C–003E не делай». И аналогично далее.

## 15. Источники и справочные материалы

**Исходный код:**
- https://github.com/edward2603-byte/Bitcop/commit/a6c07cc87755863e022867a6f87e5a30b7fd451e
- `src/server/WorkControl.Api/Program.cs`
- `src/server/WorkControl.Api/Infrastructure/WorkControlDbContext.cs`
- `src/server/WorkControl.Api/Modules/Devices/DeviceService.cs`
- `src/server/WorkControl.Api/Modules/Telemetry/TelemetryService.cs`
- `src/agent/WorkControl.Agent/AgentEventDispatcher.cs`
- `docs/TASK-003.md`, `docs/NETWORK_PILOT.md`

**Официальная документация:**
- .NET lifecycle: https://learn.microsoft.com/en-us/lifecycle/products/microsoft-net-and-net-core
- .NET support: https://learn.microsoft.com/en-us/dotnet/core/releases-and-support
- ASP.NET Core Identity GUID: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0
- Cookie authentication: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0
- CSRF: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
- Data Protection: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0
- Windows DPAPI: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata

---

**Граница исполнения:** этот документ описывает реализацию и включает небольшие фрагменты кода. Он не утверждает, что новая схема уже реализована или прошла тестирование. Все изменения в рабочем репозитории должен выполнить и проверить Codex в отдельных итерациях.


<!-- END_DETAILED_DESIGN -->

---

# Контроль полноты единого файла

В этом документе последовательно представлены:

- единая инструкция запуска и ограничения чтения контекста;
- полный TASK-003A (обновление платформы);
- полный TASK-003B (пользователи и права);
- полный TASK-003C (защищённые агенты);
- полный TASK-003D (HTTPS и сетевой пилот);
- полный TASK-003E (приёмка и выпуск);
- детальный технический проект, содержащий разделы 0–15, схемы, модели, C#-примеры, API, безопасность и тестовую матрицу.

Никаких отдельных файлов из первоначального ZIP для чтения этого документа не требуется. Результаты выполнения каждой задачи Codex по-прежнему следует сохранять **отдельно в репозитории** в `docs/TASK-003X-RESULT.md`: это выходные отчёты, а не части входного задания.
