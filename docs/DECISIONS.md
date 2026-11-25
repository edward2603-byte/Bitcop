# Журнал архитектурных решений

## ADR-001 Modular monolith

Сервер остаётся единым ASP.NET Core приложением с модулями Organization, Identity, Devices, Telemetry, Activity, Reports и Audit. Микросервисы и Kubernetes не используются.

## ADR-002 PostgreSQL и SQLite

SQLite поддерживается для LOCAL и очереди агента; PostgreSQL предусмотрен для серверного пилота. EF migrations не удаляют базу. Runtime не использует `EnsureDeleted`.

## ADR-003 Идемпотентные события

Каждое событие имеет GUID `EventId` и уникальный индекс. Повтор возвращает `duplicate=true`; конкурентный конфликт уникальности также трактуется как повтор.

## ADR-004 Граница мониторинга

Нет сбора скриншотов, веб-истории, клавиш, поисковых запросов и содержимого ввода. Расширение требует отдельной задачи и правового согласования.

## ADR-005 Раздельная аутентификация

Browser использует ASP.NET Core Identity cookie и CSRF. Агент использует отдельную схему `WorkControl KeyId.Secret`; сервер извлекает DeviceId из claims и не доверяет телу события.

## ADR-006 Секреты агента

Enrollment-код одноразовый и действует 10 минут. Сервер хранит SHA-256 секрета, клиент — DPAPI CurrentUser. Настройки и локальная очередь секрета не содержат.

## ADR-007 LOCAL и PILOT

LOCAL слушает loopback и не меняет firewall. PILOT требует HTTPS, DNS и постоянный Data Protection key ring; web и API работают с одного origin. Реальная сеть разрешается только отдельной эксплуатационной процедурой.
