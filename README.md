# notCRM

notCRM — проект для работы с организационной структурой и справочными данными компании. Репозиторий объединяет веб-интерфейс на Next.js и независимо развёртываемые сервисы на ASP.NET Core.

Сейчас реализованы управление подразделениями, локациями и должностями, просмотр дерева подразделений и поиск. EmployeeService находится на стадии каркаса: настроены запуск, подключение к PostgreSQL, логирование и проверки состояния; бизнес-функции работы с сотрудниками ещё не реализованы.

## Структура репозитория

| Путь | Назначение |
| --- | --- |
| [frontend](frontend/README.md) | Веб-интерфейс: Next.js, React, TypeScript, Tailwind CSS, TanStack Query |
| [backend/DirectoryService](backend/DirectoryService/README.md) | Организационная структура, подразделения, локации и должности |
| [backend/EmployeeService](backend/EmployeeService/README.md) | Каркас сервиса сотрудников |
| [backend/Shared](backend/Shared/README.md) | Общие библиотеки: примитивы и результаты, абстракции Core, HTTP-инфраструктура |
| [scripts/seed-directory-service-db](scripts/seed-directory-service-db/README.md) | Go-утилита для заполнения базы DirectoryService тестовыми данными |
| [deploy/postgres](deploy/postgres/) | Инициализация баз данных и пользователей сервисов |
| [docker-compose-dev.yml](docker-compose-dev.yml) | Локальное окружение разработки |
| [nginx.conf](nginx.conf) | Маршрутизация запросов к интерфейсу и API |

У каждого backend-сервиса есть собственное решение `.slnx` и Dockerfile. Проекты разделены на `Domain` (доменная модель), `Core` (сценарии и абстракции), `Infrastructure.Postgres` (хранение данных), `Contracts` (DTO) и `Web` (HTTP и сборка приложения). Общие настройки .NET находятся в [backend/Directory.Build.props](backend/Directory.Build.props), версии пакетов — в [backend/Directory.Packages.props](backend/Directory.Packages.props).

## Запуск через Docker Compose

### Требования

- Docker с запущенным движком и Docker Compose v2 с поддержкой BuildKit secrets.
- Доступ к GitHub Packages для восстановления пакетов `GodlevskiyDev.*` из источника в [backend/nuget.config](backend/nuget.config).
- Свободные порты `80`, `3000`, `8010`, `8020`, `8081` и выбранный порт PostgreSQL.

Для запуска всего окружения через Docker локальные .NET SDK, Node.js и Go не требуются.

### Подготовка настроек

Все команды этого раздела выполняются из корня репозитория.

```bash
cp .env.example .env
```

Заполните `.env` по образцу:

| Переменная | Значение |
| --- | --- |
| `POSTGRES_PORT` | Порт PostgreSQL на хосте, например `5432` вместо `your_port` |
| `POSTGRES_USER`, `POSTGRES_PASSWORD` | Учётные данные администратора PostgreSQL для создания баз и пользователей |
| `DIRECTORY_POSTGRES_DB`, `DIRECTORY_POSTGRES_USER`, `DIRECTORY_POSTGRES_PASSWORD` | База и отдельный пользователь DirectoryService |
| `EMPLOYEE_POSTGRES_DB`, `EMPLOYEE_POSTGRES_USER`, `EMPLOYEE_POSTGRES_PASSWORD` | База и отдельный пользователь EmployeeService |
| `GITHUB_NUGET_CREDENTIALS` | Строка `Username=<github-login>;Password=<token>;ValidAuthenticationTypes=Basic` с доступом к пакетам |

Замените все значения-заглушки. `.env` исключён из Git; токены и пароли не следует добавлять в репозиторий. Docker Compose передаёт `GITHUB_NUGET_CREDENTIALS` в сборку backend через BuildKit secret.

### Сборка и запуск

```bash
docker compose -f docker-compose-dev.yml up -d --build
docker compose -f docker-compose-dev.yml ps -a
```

Окружение включает PostgreSQL 17, Redis 8, Seq, оба backend-сервиса, frontend и Nginx. Сначала создаются базы и пользователи сервисов, затем контейнеры `directory-service-migrations` и `employee-service-migrations` применяют миграции соответствующих сервисов. Каждый backend-сервис запускается после успешного завершения своего мигратора. Успешное завершение контейнеров инициализации и миграций с кодом `0` — ожидаемое поведение.

Открывайте приложение через **[http://localhost](http://localhost)**: Nginx обслуживает интерфейс и перенаправляет `/api/organization/` в DirectoryService, а `/api/employees/` — в EmployeeService. В Docker-сборке frontend адрес API задан как `/api`.

| Компонент | Адрес |
| --- | --- |
| Приложение через Nginx | [http://localhost](http://localhost) |
| DirectoryService напрямую | [http://localhost:8010](http://localhost:8010) |
| Документация API DirectoryService (Scalar) | [http://localhost:8010/scalar/v1](http://localhost:8010/scalar/v1) |
| EmployeeService напрямую | [http://localhost:8020](http://localhost:8020) |
| Seq — просмотр логов | [http://localhost:8081](http://localhost:8081) |
| Frontend напрямую | [http://localhost:3000](http://localhost:3000) |

Порт `3000` даёт прямой доступ к frontend; маршруты `/api/*` обслуживает Nginx на порту `80`. PostgreSQL доступен на `localhost:${POSTGRES_PORT}`, Redis — только внутри сети Compose.

Проверить готовность сервисов можно командами:

```bash
curl --fail http://localhost:8010/health/ready
curl --fail http://localhost:8020/health/ready
```

У обоих сервисов также есть `/health/live`. Для диагностики запуска:

```bash
docker compose -f docker-compose-dev.yml logs --tail=100 directory-service-migrations directory-service employee-service
```

Остановка окружения:

```bash
docker compose -f docker-compose-dev.yml down
```

Данные PostgreSQL, Redis и Seq сохраняются в Docker volumes. Параметр `down -v` удаляет эти тома вместе с данными. Изменение переменных PostgreSQL в `.env` само по себе не переинициализирует существующий том; настройки нужно согласовывать с уже созданными базами и пользователями.

Это окружение предназначено для локальной разработки: сервисы запускаются в режиме `Development`, Seq настроен без аутентификации.

## Разработка и проверки

### Backend

Для работы вне Docker нужен .NET SDK, совместимый с [backend/global.json](backend/global.json): базовая версия `10.0.301`, политика `latestFeature`. Все проекты используют `net10.0`; предупреждения сборки считаются ошибками.

Для восстановления пакетов вне Docker задайте в текущем терминале переменную `NuGetPackageSourceCredentials_github` в том же формате, что и `GITHUB_NUGET_CREDENTIALS`. `dotnet` не загружает корневой `.env` автоматически.

Из каталога `backend/DirectoryService`:

```bash
dotnet restore DirectoryService.slnx
dotnet build DirectoryService.slnx --configuration Debug --disable-build-servers -m:1
dotnet test DirectoryService.slnx --no-build
```

Интеграционные тесты DirectoryService используют Testcontainers: нужен запущенный Docker, тестовые PostgreSQL и Redis создаются автоматически. Аналогично можно собирать решения `backend/EmployeeService/EmployeeService.slnx` и `backend/Shared/Shared.slnx`; в Shared также есть тесты.

### Frontend

Для локальной разработки используйте Node.js 24, как в Dockerfile, и npm. Из каталога `frontend`:

```bash
npm ci
cp .env.example .env
```

Укажите в `frontend/.env` адрес API через запущенный Nginx:

```dotenv
NEXT_PUBLIC_API_URL=http://localhost/api
```

Затем запустите сервер разработки:

```bash
npm run dev
```

Если контейнерный frontend уже занимает порт `3000`, используйте `npm run dev -- --port 3001`. В этом варианте backend и Nginx продолжают работать через Compose.

При открытии локального frontend на порту `3000` или `3001` обращения к `http://localhost/api` идут на другой origin. В текущей конфигурации CORS и проксирование API через Next.js не настроены; для полноценной работы интерфейса с API используйте запуск всего приложения через Compose либо предварительно настройте прокси или CORS для локальной разработки.

Доступные проверки:

```bash
npm run lint
npm run build
```

Для E2E-проверок подготовлены команда `npm run test:e2e` и конфигурация Playwright, но каталог `frontend/tests` пока отсутствует. После добавления тестов установите браузер командой `npx playwright install chromium`. Playwright самостоятельно запускает сервер разработки на порту `3112`; этот порт должен быть свободен.

## Работа с изменениями

Изменения ведутся в отдельных ветках от `main` и проходят через pull request. Для коммитов используется Conventional Commits, например `docs: add root readme`.

Инструкции для AI-агентов находятся в [AGENTS.md](AGENTS.md); дополнительные правила компонентов — в [backend/DirectoryService/AGENTS.md](backend/DirectoryService/AGENTS.md) и [frontend/AGENTS.md](frontend/AGENTS.md).
