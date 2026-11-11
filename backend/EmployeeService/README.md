# EmployeeService

EmployeeService — каркас будущего сервиса сотрудников notCRM. Сейчас реализованы запуск ASP.NET Core, подключение к PostgreSQL, отправка логов в Seq и проверки состояния. Доменные модели сотрудников, бизнес-сценарии и HTTP API для работы с сотрудниками пока отсутствуют.

Подготовка общего окружения описана в [корневом README](../../README.md).

## Структура

Решение — [EmployeeService.slnx](EmployeeService.slnx).

| Проект | Текущее состояние |
| --- | --- |
| [Web](src/EmployeeService.Web/) | Точка входа, настройка DI, Serilog, HTTP и health checks |
| [Infrastructure.Postgres](src/EmployeeService.Infrastructure.Postgres/) | Регистрация Npgsql и пустой `EmployeeServiceDbContext` |
| [Domain](src/EmployeeService.Domain/) | Проект для доменной модели, пока без реализации |
| [Core](src/EmployeeService.Core/) | Проект для бизнес-сценариев, пока без реализации |
| [Contracts](src/EmployeeService.Contracts/) | Проект для DTO, пока без реализации |

Web сейчас ссылается на Infrastructure.Postgres. Миграций, таблиц сотрудников и тестовых проектов в сервисе пока нет.

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

Миграции для текущего пустого контекста не предусмотрены: сервису нужна существующая доступная база, схема сотрудников пока не создаётся.

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

Автоматических тестов в решении пока нет. Доступная проверка исходников — сборка решения; предупреждения считаются ошибками через общий [Directory.Build.props](../Directory.Build.props).
