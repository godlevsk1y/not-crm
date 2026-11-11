# Shared

Общие библиотеки backend-сервисов notCRM. Это решение с исходниками NuGet-пакетов, а не отдельный микросервис: у него нет HTTP-сервера, базы данных или Dockerfile.

## Состав

| Проект | Назначение |
| --- | --- |
| [Shared.Kernel](Shared.Kernel/) | Ошибки `Error`, `ErrorMessage`, `ErrorType`, общие ошибки и результат постраничной выборки `PagedResult` |
| [Shared.Core](Shared.Core/) | Интерфейсы команд, запросов и обработчиков, абстракции транзакций, регистрация обработчиков и валидаторов, преобразование ошибок валидации |
| [Shared.Framework](Shared.Framework/) | HTTP-обёртка `Envelope`, результаты `EndpointResults`, middleware обработки исключений |
| [Shared.Framework.Tests](Shared.Framework.Tests/) | Тесты обработки исключений |

Пакеты имеют идентификаторы `GodlevskiyDev.Shared.Kernel`, `GodlevskiyDev.Shared.Core` и `GodlevskiyDev.Shared.Framework`; пространства имён в коде начинаются с `Shared`. Core и Framework используют Kernel через NuGet. При изменении локального Kernel зависимые проекты не переключаются автоматически на его исходники.

## Сборка и тесты

Нужен .NET SDK, совместимый с [global.json](../global.json): базовая версия `10.0.301`, политика `latestFeature`. Целевая платформа — `net10.0`. Настройки анализаторов и обработки предупреждений находятся в [Directory.Build.props](../Directory.Build.props), версии зависимостей — в [Directory.Packages.props](../Directory.Packages.props).

Настройте доступ к GitHub Packages согласно [корневому README](../../README.md). Для локального `dotnet restore` используется переменная окружения `NuGetPackageSourceCredentials_github`; корневой `.env` автоматически не загружается.

Из каталога `backend/Shared`:

```bash
dotnet restore Shared.slnx
dotnet build Shared.slnx --configuration Debug --disable-build-servers -m:1
dotnet test Shared.slnx --no-build
```

Тестам этого решения не нужны контейнеры PostgreSQL или Redis.

## Использование и изменения пакетов

Сервисы подключают библиотеки через `PackageReference`, версии централизованно задаются в `Directory.Packages.props`, источник GitHub Packages — в [nuget.config](../nuget.config). Поэтому изменение исходников в этом каталоге само по себе не меняет поведение сервисов, использующих опубликованную версию пакета.

Версия и метаданные каждого пакета заданы в его `.csproj`. Для подготовки пакетов локально:

```bash
dotnet pack Shared.slnx --configuration Release --output ./artifacts --disable-build-servers -m:1
```

Команда создаёт пакеты, но не публикует их. При выпуске изменений необходимо согласовать версии пакетов и зависимости потребителей.

Общие библиотеки содержат механизмы, используемые разными сервисами; бизнес-правила подразделений или сотрудников остаются в соответствующем сервисе.
