# EmployeeService

EmployeeService — сервис учёта сотрудников notCRM (в активной разработке).

Подготовка общего окружения описана в [корневом README](../../README.md).

## Структура

Решение — [EmployeeService.slnx](EmployeeService.slnx).

| Проект | Текущее состояние |
| --- | --- |
| [Web](src/EmployeeService.Web/) | Точка входа, настройка DI, Serilog, HTTP и health checks |
| [Infrastructure.Postgres](src/EmployeeService.Infrastructure.Postgres/) | Фабрика подключений, регистрация Npgsql, конфигурация Employee и начальная миграция |
| [Domain](src/EmployeeService.Domain/) | Employee, EmployeeName, типизированные идентификаторы и доменные правила |
| [Core](src/EmployeeService.Core/) | Запрос GetEmployeeById, валидация, SQL и маппинг карточки через Dapper |
| [Contracts](src/EmployeeService.Contracts/) | DTO карточки сотрудника |

Web регистрирует Core и Infrastructure.Postgres. Core использует Contracts и общие CQRS-интерфейсы из `GodlevskiyDev.Shared.Core`; обработчик GetEmployeeById выполняет SQL через Dapper и формирует DTO. Infrastructure.Postgres реализует фабрику подключений Core через существующий `NpgsqlDataSource` и сохраняет доменную модель через EF Core. Таблица `employees` хранит имена, фамилии и внешние идентификаторы должностей и подразделений без межсервисных внешних ключей. Глобальный фильтр EF Core и явное условие `deleted_at IS NULL` в Dapper скрывают мягко удалённых сотрудников; уволенные сотрудники остаются видимыми. Значения времени в UTC задаются доменными методами.

## Зависимости и конфигурация

Для работы вне Docker нужен .NET SDK, совместимый с [global.json](../global.json): `10.0.301` с политикой `latestFeature`. В окружении Compose используются PostgreSQL 17 и Seq. Redis этому сервису не требуется.

Основные настройки находятся в [appsettings.json](src/EmployeeService.Web/appsettings.json). Параметры подключения передавайте через окружение; `dotnet run` не загружает корневой `.env` автоматически.

| Переменная | Назначение |
| --- | --- |
| `ConnectionStrings__EmployeeServiceDbContext` | Подключение к PostgreSQL |
| `Serilog__WriteTo__1__Args__serverUrl` | Адрес Seq для логов; локально по умолчанию `http://localhost:8081` |
| `ASPNETCORE_ENVIRONMENT` | Среда приложения; в Compose и профиле `http` используется `Development` |

Не добавляйте реальные пароли и токены в файлы репозитория.

## Запуск через Docker Compose

Подготовьте корневой `.env` по [инструкции](../../README.md#подготовка-настроек). Из корня репозитория:

```bash
docker compose -f docker-compose-dev.yml up -d --build employee-service
```

Compose запустит PostgreSQL и Seq, создаст отдельную базу и пользователя из `EMPLOYEE_POSTGRES_*`, затем запустит сервис. Сборка Docker требует secret `GITHUB_NUGET_CREDENTIALS`, как задано в общем Compose-файле.

Сервис доступен на `http://localhost:8020`. При полном запуске окружения Nginx перенаправляет `/api/employees/` в корень EmployeeService, удаляя этот префикс.

## Запуск вне Docker

Следующие команды выполняются из `backend/EmployeeService`:

```bash
dotnet restore EmployeeService.slnx
dotnet build EmployeeService.slnx --configuration Debug --disable-build-servers -m:1
```

Источники пакетов настроены в [nuget.config](../nuget.config). Если восстановление требует авторизации в GitHub Packages, задайте переменную `NuGetPackageSourceCredentials_github` в формате `Username=<github-login>;Password=<token>;ValidAuthenticationTypes=Basic`.

Подготовьте базу PostgreSQL и задайте подключение, заменив значения в угловых скобках:

```bash
export ConnectionStrings__EmployeeServiceDbContext='Host=localhost;Port=<port>;Database=<database>;Username=<user>;Password=<password>'
dotnet run --project src/EmployeeService.Web --launch-profile http
```

При использовании PostgreSQL из Compose укажите опубликованный `POSTGRES_PORT` и параметры базы и пользователя `EMPLOYEE_POSTGRES_*`. Профиль `http` слушает `http://localhost:5043`. Seq из Compose доступен на `http://localhost:8081`.

## HTTP и проверки состояния

| Метод и путь | Ответ / проверка |
| --- | --- |
| `GET /` | Технический ответ `Hello World!` |
| `GET /{id:guid}` | Карточка сотрудника в `Envelope<EmployeeDto>`; через Nginx — `GET /api/employees/{id}` |
| `GET /health/live` | Работа процесса |
| `GET /health/ready` | Возможность подключения к PostgreSQL |

Seq не входит в проверку готовности.

Карточка содержит `id`, `firstName`, `lastName`, `positionId`, `departmentId`, `hiredAt`, `dismissedAt` и `updatedAt`. Идентификаторы должности и подразделения возвращаются как UUID, без названий и обращений в DirectoryService. Даты имеют формат `yyyy-MM-dd`, время изменения — UTC; `dismissedAt` равен `null`, если сотрудник не уволен. `deletedAt` не возвращается.

Существующий сотрудник, включая уволенного, получает `200 OK`. Неизвестный или мягко удалённый сотрудник — `404 Not Found` с кодом `employee.not.found`. Пустой UUID — `400 Bad Request` с ошибкой валидации `employee.id.empty` для поля `Id`. Ответы используют общий envelope из `Shared.Framework`. Некорректный UUID не соответствует ограничению маршрута `:guid` и получает `404` без вызова обработчика.

OpenAPI и Scalar доступны во всех средах, кроме `Production`:

| Документация | Локальный запуск | Docker |
| --- | --- | --- |
| OpenAPI | [Спецификация](http://localhost:5043/openapi/v1.json) | [Спецификация](http://localhost:8020/openapi/v1.json) |
| Scalar | [Просмотр API](http://localhost:5043/scalar/v1) | [Просмотр API](http://localhost:8020/scalar/v1) |

В `Production` оба маршрута документации не публикуются. OpenAPI описывает DTO карточки и ответы `200`, `400`, `404`.

Проверка Docker-окружения:

```bash
curl --fail http://localhost:8020/
curl --fail http://localhost:8020/health/ready
docker compose -f docker-compose-dev.yml logs --tail=100 employee-service
```

Команду просмотра логов выполняйте из корня репозитория. Для локального процесса используйте порт `5043` вместо `8020`.

## Миграции

Команды выполняются из `backend/EmployeeService` с переменной `ConnectionStrings__EmployeeServiceDbContext`, заданной выше. При необходимости установите инструмент EF:

```bash
dotnet tool install --global dotnet-ef --version 10.0.11
```

Начальная миграция уже включена в репозиторий. При генерации следующей миграции замените `Initial` новым именем:

```bash
dotnet ef migrations add Initial \
  --project src/EmployeeService.Infrastructure.Postgres \
  --startup-project src/EmployeeService.Web \
  --context EmployeeServiceDbContext \
  --output-dir Migrations
```

Применение миграций и проверка соответствия модели её снимку:

```bash
dotnet ef database update \
  --project src/EmployeeService.Infrastructure.Postgres \
  --startup-project src/EmployeeService.Web \
  --context EmployeeServiceDbContext

dotnet ef migrations has-pending-model-changes \
  --project src/EmployeeService.Infrastructure.Postgres \
  --startup-project src/EmployeeService.Web \
  --context EmployeeServiceDbContext
```

Для отката на изолированной базе выполните `dotnet ef database update 0` с теми же аргументами `--project`, `--startup-project` и `--context`, затем повторите `database update` для применения миграций. Для генерации нужна корректная строка подключения; для применения — доступная база данных. Приложение не применяет миграции автоматически.

## Тесты

Из `backend/EmployeeService`:

```bash
dotnet test EmployeeService.slnx --configuration Debug
```

Модульные тесты проверяют доменные правила. Предупреждения считаются ошибками через общий [Directory.Build.props](../Directory.Build.props).


Интеграционные тесты требуют запущенный Docker (например, Docker Desktop) и .NET SDK, указанный выше. Из корня репозитория:

```bash
dotnet test backend/EmployeeService/EmployeeService.slnx
```

Только интеграционные тесты:

```bash
dotnet test backend/EmployeeService/tests/EmployeeService.IntegrationTests/EmployeeService.IntegrationTests.csproj
```

`EmployeeServiceTestWebFactory` используется через `IClassFixture<EmployeeServiceTestWebFactory>`: запускает изолированный PostgreSQL (`postgres:17`) со случайным портом, перенастраивает `NpgsqlDataSource` приложения и применяет существующие EF Core миграции. Локальная или общая база и Compose для тестов не нужны. При первом запуске Docker загружает образ PostgreSQL.

Восемь HTTP-сценариев GetById из issue #158 находятся в одном файле [GetEmployeeByIdTests.cs](tests/EmployeeService.IntegrationTests/Employees/Queries/GetEmployeeByIdTests.cs). Проверки используют FluentAssertions, данные готовятся через EF Core без POST-эндпоинта. Тесты одного класса выполняются последовательно и очищают базу перед каждым сценарием. DirectoryService для запуска не нужен.

Перед каждым тестом вызывайте `ResetDatabaseAsync()` (например, в `IAsyncLifetime.InitializeAsync()` тестового класса). Respawn очищает данные, включая мягко удалённых сотрудников, сохраняя схему и `__EFMigrationsHistory`. Тесты, использующие одну фабрику и базу, должны выполняться последовательно. После завершения fixture автоматически освобождает приложение и контейнер.
