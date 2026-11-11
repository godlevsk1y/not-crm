# DirectoryService

DirectoryService управляет организационной структурой notCRM: подразделениями и их иерархией, локациями, должностями и связями между ними. Сервис предоставляет HTTP API для создания, изменения, удаления и восстановления справочных данных, просмотра дерева и поиска.

Общий запуск приложения и подготовка доступа к GitHub Packages описаны в [корневом README](../../README.md).

## Структура

Решение — [DirectoryService.slnx](DirectoryService.slnx).

| Проект | Ответственность |
| --- | --- |
| [Domain](src/DirectoryService.Domain/) | Сущности, идентификаторы, объекты-значения и доменные ограничения |
| [Core](src/DirectoryService.Core/) | Команды и запросы, обработчики, валидация, интерфейсы репозиториев и кеширование |
| [Infrastructure.Postgres](src/DirectoryService.Infrastructure.Postgres/) | EF Core, Dapper, PostgreSQL, репозитории, транзакции и миграции |
| [Contracts](src/DirectoryService.Contracts/) | DTO запросов и ответов |
| [Web](src/DirectoryService.Web/) | Контроллеры, конфигурация, логирование, OpenAPI и проверки состояния |
| [IntegrationTests](tests/DirectoryService.IntegrationTests/) | Интеграционные тесты HTTP API и инфраструктуры |

Общие абстракции и HTTP-механизмы подключаются через NuGet-пакеты `GodlevskiyDev.Shared.*`. Версии зависимостей заданы в [Directory.Packages.props](../Directory.Packages.props).

## Зависимости и конфигурация

Для локальной разработки нужен .NET SDK, совместимый с [global.json](../global.json) (`10.0.301`, `latestFeature`). Сервис использует PostgreSQL 17, Redis 8 и Seq. При отсутствии строки подключения Redis HybridCache работает без распределённого Redis-кеша.

Настройки приложения находятся в [appsettings.json](src/DirectoryService.Web/appsettings.json). Конфиденциальные значения передавайте через переменные окружения или .NET User Secrets. Корневой `.env` автоматически читает Docker Compose, но не `dotnet run`.

| Переменная окружения | Назначение |
| --- | --- |
| `ConnectionStrings__DirectoryServiceDbContext` | Подключение к базе PostgreSQL сервиса |
| `ConnectionStrings__Redis` | Подключение распределённого кеша; в Compose — `redis:6379` |
| `Seq__ServerUrl` | Адрес Seq для проверки готовности; локально по умолчанию `http://localhost:8081` |
| `Serilog__WriteTo__1__Args__serverUrl` | Адрес отправки логов в Seq; локально по умолчанию `http://localhost:8081` |
| `ASPNETCORE_ENVIRONMENT` | Среда приложения; профиль `http` использует `Development` |

При изменении адреса Seq задайте оба параметра: адрес проверки готовности и адрес приёмника логов.

### Docker Compose

Подготовьте корневой `.env` по [инструкции](../../README.md#подготовка-настроек). Из корня репозитория:

```bash
docker compose -f docker-compose-dev.yml up -d --build directory-service
```

Compose запустит необходимые зависимости, создаст базу и пользователя и применит миграции отдельным контейнером `directory-service-migrations`. API будет доступен на `http://localhost:8010`. Для работы через Nginx и frontend используйте полный запуск из корневого README.

### Запуск вне Docker

Все следующие команды выполняются из `backend/DirectoryService`.

Перед восстановлением пакетов задайте `NuGetPackageSourceCredentials_github` в формате `Username=<github-login>;Password=<token>;ValidAuthenticationTypes=Basic`. Токен должен предоставлять доступ к используемым пакетам GitHub Packages.

```bash
dotnet restore DirectoryService.slnx
dotnet build DirectoryService.slnx --configuration Debug --disable-build-servers -m:1
```

Подготовьте базу PostgreSQL и задайте её параметры, заменив значения в угловых скобках:

```bash
export ConnectionStrings__DirectoryServiceDbContext='Host=localhost;Port=<port>;Database=<database>;Username=<user>;Password=<password>'
```

Если инфраструктура запущена через Compose, используйте опубликованный `POSTGRES_PORT` и учётные данные `DIRECTORY_POSTGRES_*`. Seq доступен на порту `8081`. Redis в Compose не публикует порт на хост: для локального процесса используйте отдельный доступный Redis либо не задавайте `ConnectionStrings__Redis`.

Примените миграции по инструкции ниже и запустите приложение:

```bash
dotnet run --project src/DirectoryService.Web --launch-profile http
```

Профиль `http` слушает `http://localhost:5231`. Сервис сам не применяет миграции при обычном запуске Web-приложения.

## Миграции

Миграции находятся в [Infrastructure.Postgres/Migrations](src/DirectoryService.Infrastructure.Postgres/Migrations/). Для команд ниже нужен `dotnet-ef`; Dockerfile использует версию `10.0.11`.

```bash
dotnet tool install --global dotnet-ef --version 10.0.11
```

После настройки подключения к целевой базе:

```bash
dotnet ef database update \
  --project src/DirectoryService.Infrastructure.Postgres \
  --startup-project src/DirectoryService.Web
```

Создание миграции после изменения модели:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/DirectoryService.Infrastructure.Postgres \
  --startup-project src/DirectoryService.Web \
  --output-dir Migrations
```

Перед применением проверьте целевую базу и сгенерированный SQL/изменения схемы. В Compose эта операция уже выполняется контейнером миграций; повторять её вручную для обычного запуска не требуется.

## HTTP API

Пути в таблице указаны относительно корня сервиса. Через Nginx к ним добавляется префикс `/api/organization`.

| Путь | Возможности |
| --- | --- |
| `/departments` | Создание и список подразделений, фильтрация и пагинация |
| `/departments/{id}` | Чтение, изменение и удаление подразделения |
| `/departments/{id}/parent` | Перенос подразделения к другому родителю |
| `/departments/tree`, `/departments/tree/search` | Дерево подразделений и поиск по нему |
| `/departments/{id}/children`, `/descendants`, `/ancestors`, `/branch` | Дочерние узлы, потомки, предки и ветка; последние три суффикса также относятся к `/departments/{id}` |
| `/departments/{id}/locations`, `/departments/{id}/positions` | Связи подразделений с локациями и должностями |
| `/locations`, `/positions` | Создание, список и изменение справочных данных; удаление и восстановление по идентификатору |
| `/search`, `/search/{entityType}` | Общий поиск и постраничный поиск по типу сущности |

Подробные методы, параметры и схемы доступны в Scalar: [Docker](http://localhost:8010/scalar/v1) или [локальный запуск](http://localhost:5231/scalar/v1). OpenAPI доступен по `/openapi/v1.json`. Эти страницы включены вне среды `Production`.

Пример запроса к Docker-окружению:

```bash
curl --fail 'http://localhost:8010/departments?page=1&pageSize=10'
```

### Состояние и логи

- `/health/live` проверяет работу процесса.
- `/health/ready` проверяет доступность PostgreSQL и Seq. Redis в эту проверку не включён.
- Логи отправляются в консоль и Seq; при запуске через Compose интерфейс Seq доступен на `http://localhost:8081`.

## Особенности данных

Удаление справочных сущностей логическое; API предусматривает восстановление через `POST /{resource}/{id}/restore`. Фоновая очистка физически удаляет подходящие удалённые записи с учётом связей. По умолчанию срок хранения — 30 дней, интервал очистки — 10 минут, размер пакета — 1000; параметры заданы в секции `DatabaseCleanupOptions`.

Иерархия подразделений хранится с путями PostgreSQL `ltree`. Обработчики изменения slug и переноса подразделения пересчитывают пути потомков и инвалидируют затронутый кеш. Настройки времени жизни кеша находятся в секции `HybridCache`.

## Проверки

```bash
dotnet build DirectoryService.slnx --configuration Debug --disable-build-servers -m:1
dotnet test DirectoryService.slnx --no-build
```

Тесты используют xUnit, WebApplicationFactory, Testcontainers и Respawn. Нужен запущенный Docker: тесты самостоятельно создают PostgreSQL и Redis, применяют миграции и очищают данные между сценариями. Рабочая база из Compose для этого не используется.

Для наполнения локальной базы есть [отдельная утилита](../../scripts/seed-directory-service-db/).
