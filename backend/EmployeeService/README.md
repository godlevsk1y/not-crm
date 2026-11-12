# EmployeeService

EmployeeService implements the Employee domain model, immutable EmployeeName and typed identifiers. EF Core persists employees in PostgreSQL. Application use cases and employee HTTP APIs are not implemented yet.

Подготовка общего окружения описана в [корневом README](../../README.md).

## Структура

Решение — [EmployeeService.slnx](EmployeeService.slnx).

| Проект | Текущее состояние |
| --- | --- |
| [Web](src/EmployeeService.Web/) | Точка входа, настройка DI, Serilog, HTTP и health checks |
| [Infrastructure.Postgres](src/EmployeeService.Infrastructure.Postgres/) | Npgsql registration, Employee mapping and initial migration |
| [Domain](src/EmployeeService.Domain/) | Employee, EmployeeName, typed IDs and domain rules |
| [Core](src/EmployeeService.Core/) | Проект для бизнес-сценариев, пока без реализации |
| [Contracts](src/EmployeeService.Contracts/) | Проект для DTO, пока без реализации |

Web references Infrastructure.Postgres, which references Domain. The `employees` table stores names and external assignment IDs without cross-service foreign keys. The global query filter hides soft-deleted employees; dismissed employees remain visible. Domain methods supply UTC timestamps.

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
| `GET /health/live` | Работа процесса |
| `GET /health/ready` | Возможность подключения к PostgreSQL |

Seq не входит в проверку готовности. OpenAPI и Scalar в сервисе пока не подключены.

Проверка Docker-окружения:

```bash
curl --fail http://localhost:8020/
curl --fail http://localhost:8020/health/ready
docker compose -f docker-compose-dev.yml logs --tail=100 employee-service
```

Команду просмотра логов выполняйте из корня репозитория. Для локального процесса используйте порт `5043` вместо `8020`.

## Migrations

Run from `backend/EmployeeService` with `ConnectionStrings__EmployeeServiceDbContext` set as above. Install the EF tool if necessary:

```bash
dotnet tool install --global dotnet-ef --version 10.0.11
```

The initial migration is already checked in. To generate a future migration, replace `Initial` with a new name:

```bash
dotnet ef migrations add Initial \
  --project src/EmployeeService.Infrastructure.Postgres \
  --startup-project src/EmployeeService.Web \
  --context EmployeeServiceDbContext \
  --output-dir Migrations
```

Apply migrations and check snapshot consistency:

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

On an isolated database, roll back with `dotnet ef database update 0` using the same project/startup-project/context arguments, then run `database update` again to reapply. Generation requires a valid connection string; applying migrations requires a reachable database. The application does not apply migrations automatically.

## Tests

From `backend/EmployeeService`:

```bash
dotnet test EmployeeService.slnx --configuration Debug
```

Unit tests cover domain rules. Warnings are errors through [Directory.Build.props](../Directory.Build.props).
