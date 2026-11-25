# Результат TASK-003B

Реализованы AppUser/Identity с GUID, роли `ADMIN`, `MAIN_USER`, `GROUP_MANAGER`, `UserGroupAccess`, cookie HttpOnly/SameSite, lockout, rate limit, CSRF, login/logout/me и аудит. Dashboard требует cookie и ограничивает группы внутри SQL-запроса. Первый ADMIN создаётся только локальной одноразовой CLI-процедурой; публичной регистрации нет. React получил страницу входа и явные 401/403/offline состояния, демо-fallback удалён.

SQLite переведён с `EnsureCreated` на EF migrations с baseline старой базы. Data Protection key ring хранится вне исходников. Чистая SQLite-база и сохранение данных legacy-копии проверяются отдельно в итоговой матрице.

Ограничение: UI создания пользователей и назначения групп не входит в этот этап; серверная модель и проверка group scope покрыты автоматическим тестом.
