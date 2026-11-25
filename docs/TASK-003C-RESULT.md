# Результат TASK-003C

Реализованы одноразовые enrollment-коды с TTL 10 минут и rate limit, Pending/Active/Suspended/Revoked, ручное одобрение ADMIN, 256-битные credentials, хранение хэшей, отдельная agent authentication scheme, ротация и отзыв. DeviceId определяется сервером по credential. Legacy anonymous endpoints удалены.

Агент сохраняет secret через DPAPI, не пишет его в настройки/очередь, не отключает TLS и сохраняет события offline. Проверены Pending-отказ, одобрение, повтор кода, дедупликация, ротация, отзыв и двухчасовая очередь.

Ограничение: плановая автоматическая ротация в UI агента не включена; транспорт и серверный endpoint реализованы.
