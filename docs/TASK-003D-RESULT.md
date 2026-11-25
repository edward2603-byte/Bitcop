# Результат TASK-003D

LOCAL остаётся loopback-only и не меняет firewall. Добавлены `prepare-pilot.ps1` и `start-pilot.ps1`: same-origin пакет API+React, HTTPS PFX, DNS-имя, постоянный Data Protection key ring и fail-closed startup validation. Валидатор требует совпадения HTTPS URL и DNS, защищённый PFX/P12 с паролем и абсолютный key ring вне репозитория. Agent принимает сетевой адрес только с HTTPS. Размер запросов и частота login/enrollment ограничены, CSRF-ошибки возвращают безопасный 400.

Никакие сетевые правила, DNS-записи и сертификаты автоматически не создавались. Реальный двухкомпьютерный HTTPS-тест не проверен: в текущей среде отсутствуют второй ПК, согласованный DNS, CA/PFX и разрешённый тестовый сегмент. До этого реальные данные запрещены.
