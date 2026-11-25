# Архитектура WorkControl

```text
WPF agent -- WorkControl credential / HTTPS --> ASP.NET Core modular monolith --> SQLite/PostgreSQL
    |                                                   |
    +-- SQLite offline queue                            +-- Identity cookie + CSRF --> React UI
```

## Поток агента

1. ADMIN создаёт одноразовый enrollment-код.
2. Агент вызывает `POST /api/agent/v1/enroll`, получает DeviceId/KeyId/Secret и сохраняет секрет через DPAPI.
3. Устройство имеет статус Pending и не может передавать телеметрию.
4. ADMIN назначает существующего сотрудника и переводит устройство в Active.
5. Агент отправляет очередь на `POST /api/agent/v1/events`; DeviceId берётся из аутентификационных claims.
6. Ротация немедленно отзывает старый credential; отзыв устройства блокирует все его credentials.

Legacy endpoints самостоятельной регистрации и анонимной телеметрии отсутствуют.

## Поток панели

Первый ADMIN создаётся только локальной CLI-процедурой. Вход защищён lockout/rate-limit, cookie HttpOnly/SameSite и CSRF. `GROUP_MANAGER` получает только строки групп из `UserGroupAccess`; ограничение входит в SQL-запрос, а не применяется после загрузки.

## Режимы

- LOCAL: API `127.0.0.1:5080`, Vite `127.0.0.1:5173`, HTTP допустим только на loopback.
- PILOT: HTTPS DNS endpoint, same-origin static web, постоянный key ring, отдельный сертификат; валидатор fail-closed.

Потеря связи, статус устройства и active/idle — независимые состояния. Формулы приведены в `METRICS_SPEC.md`.
