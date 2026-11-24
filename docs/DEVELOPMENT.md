# Разработка и проверка

## Документированные зависимости

- Windows 10/11;
- .NET 8 SDK, доступный как команда `dotnet`;
- Node.js 20+ и npm;
- интернет при первом восстановлении NuGet- и npm-пакетов.

Переносимый SDK в `.tools` не является частью репозитория и не используется. Docker и PostgreSQL для локального испытания не нужны: API создаёт игнорируемый Git файл `workcontrol.db`.

## Команды проверки

```powershell
dotnet restore tests/WorkControl.Api.Tests/WorkControl.Api.Tests.csproj
dotnet build src/server/WorkControl.Api/WorkControl.Api.csproj -c Release --no-restore
dotnet build src/agent/WorkControl.Agent/WorkControl.Agent.csproj -c Release --no-restore
dotnet test tests/WorkControl.Api.Tests/WorkControl.Api.Tests.csproj -c Release --no-restore
dotnet publish src/agent/WorkControl.Agent/WorkControl.Agent.csproj -c Release --no-restore --self-contained false -p:UseAppHost=false -o release/agent

npm --prefix src/web ci --no-audit --no-fund
npm --prefix src/web test
npm --prefix src/web run build
```

Та же последовательность выполняется GitHub Actions на `windows-latest`.

## Безопасный локальный режим

`manager.bat` запускает API на `http://127.0.0.1:5080` и Vite на `http://127.0.0.1:5173`. Скрипт не повышает права, не меняет брандмауэр и не использует `0.0.0.0`. CORS разрешает только локальные адреса панели.

Ручной эквивалент:

```powershell
dotnet run --project src/server/WorkControl.Api --urls http://127.0.0.1:5080
npm --prefix src/web run dev -- --host 127.0.0.1
```

## Ручной сквозной тест

1. Запустить `manager.bat`.
2. Убедиться, что панель показывает зелёную строку «Источник: локальный API», а не жёлтое предупреждение о демонстрационных данных.
3. На том же компьютере запустить `release/agent/employee.bat`.
4. Оставить адрес `http://localhost:5080`, отправить событие и обновить панель.
5. Проверить имя пользователя, имя компьютера, время последней связи, состояние активности и статус «На связи».
6. Закрыть окно API, отправить событие агентом и убедиться в сообщении о локальном сохранении.
7. Снова запустить API и отправить следующее событие: сохранённая очередь должна уйти до текущего события.

## Конфигурация

Порог неактивности агента задаётся `WORKCONTROL_IDLE_THRESHOLD_SECONDS` (по умолчанию 60), длительного отсутствия — `WORKCONTROL_LONG_ABSENCE_THRESHOLD_SECONDS` (300). Срок статуса «На связи» на сервере задаётся `Presence:OnlineThresholdSeconds` (90), а максимальный засчитываемый промежуток между сигналами — `Telemetry:MaximumSignalGapSeconds` (90). Эти параметры независимы.

Для production автоматическое применение миграций должно быть отключено. Сетевой и production-запуск не входят в `TASK-002`.
