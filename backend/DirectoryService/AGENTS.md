# DirectoryService contributor guide

This file applies to `backend/DirectoryService` and its descendants. The repository root `AGENTS.md` still applies; this file adds service-specific context and rules.

## Service responsibility

DirectoryService is responsible for managing the organizational structure and directory data used across notCRM.

The service owns concepts such as:

- departments and their hierarchical relationships
- locations and their association with departments
- positions within the organization
- organizational naming, identifiers, and related directory metadata

DirectoryService is the source of truth for organizational structure. Other services should consume directory information through its public contracts rather than duplicating or independently managing the same data.

Keep functionality in this service when it concerns the structure of an organization or the relationships between its directory entities. Business processes that merely reference departments, locations, or positions belong in the service that owns those processes.

## Project boundaries

Keep responsibilities in the existing projects and preserve the dependency direction:

| Project | Responsibility | Typical contents |
| --- | --- | --- |
| `DirectoryService.Domain` | Business rules and domain state | Entities, strongly typed IDs, value objects, domain errors |
| `DirectoryService.Core` | Use cases and application orchestration | Feature commands/handlers/validators, repository and transaction abstractions |
| `DirectoryService.Infrastructure.Postgres` | PostgreSQL persistence | `DirectoryServiceDbContext`, EF configurations, repositories, transactions, migrations |
| `DirectoryService.Contracts` | HTTP-facing DTOs | Request and response records under `WebApi/` |
| `DirectoryService.Shared` | Cross-cutting shared primitives | `Error`, `ErrorMessage`, and `ErrorType` |
| `DirectoryService.Web` | HTTP composition and presentation | Controllers, middleware, result mapping, DI, configuration, startup |

Important dependency rules:

- Domain must not depend on Core, Infrastructure, or Web.
- Core owns abstractions such as `IDepartmentsRepository`, `ILocationsRepository`, `ITransactionManager`, and `ICommandHandler`; it must not depend on concrete PostgreSQL implementations.
- Infrastructure implements Core abstractions and is registered through `AddPostgresInfrastructure`.
- Web may compose all layers, but controllers should remain thin and should not access `DbContext` or repositories directly.
- Contracts are transport types. Do not put domain behavior or persistence concerns in request/response records.

## How to structure changes

For a new use case, follow the existing vertical feature shape in `DirectoryService.Core/Features`:

1. Add a feature folder such as `Features/Departments/CreateDepartment`.
2. Define a command implementing `ICommand`.
3. Implement `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResponse>`.
4. Add a FluentValidation validator when the request has input rules. Registering validators and handlers is convention-based in `AddCore`; do not add one-off registrations unless necessary.
5. Return `Result<T, Error>` or `UnitResult<Error>` for expected failures. Use the existing `Error` factories and service error helpers instead of throwing for validation, not-found, conflict, or domain outcomes.
6. Add or extend a repository abstraction in Core only when persistence access is required; implement it in Infrastructure.
7. Save through `ITransactionManager`. Pass the request `CancellationToken` through every async boundary.
8. Add the Web controller action that constructs the command, invokes the handler, maps failures with `EndpointResults.Error`, and returns the appropriate typed result.

Keep domain invariants in entities and value objects. Prefer `DepartmentId`, `LocationId`, `PositionId`, `DepartmentName`, `LocationName`, `PositionName`, `Slug`, `Path`, and `Address` over raw primitives at domain boundaries. Use entity methods such as `Rename`, `ChangeSlug`, and `SetParent` rather than mutating private state from outside the domain model.

When changing a department hierarchy, account for `Department.Path`: changing a slug or parent recalculates the current department path, but descendant propagation is not currently implemented. Do not silently assume that updating a parent automatically updates all descendants; address that behavior explicitly if the feature requires it.

## Error and HTTP conventions

- Define stable, machine-readable error codes in `ErrorMessage.Code`; keep human-readable text separate.
- Use `Validation`, `NotFound`, `Conflict`, `Domain`, `BadRequest`, `Failure`, or `Internal` according to the existing `ErrorType` semantics.
- Convert FluentValidation failures with `ValidationExtensions.ToError` so field-level messages retain their `InvalidField` values.
- In controllers, return `EndpointResults.Ok`, `Created`, `NoContent`, or `Error`. Do not hand-roll a different response envelope.
- `EndpointResults.Error` maps validation/domain errors to 400, not-found to 404, conflict to 409, and internal/failure to 500. Update this centralized mapping if a new error category needs a different HTTP status.
- Unexpected exceptions are logged and converted by `ExceptionMiddleware`; do not expose exception details or connection strings in API responses.

## Persistence and migrations

`DirectoryServiceDbContext` discovers configurations from the Infrastructure assembly. Keep EF mapping in `Infrastructure.Postgres/Configurations`, repository implementations in `Repositories`, and transaction code in `Transactions`.

When the domain or persistence model changes:

- Update the domain model first, then the EF configuration and repository queries as needed.
- Create a new migration; do not edit an applied migration or the model snapshot by hand.
- Review the generated migration for destructive operations, nullable changes, indexes, foreign keys, and data-loss risk before applying it.
- Keep migrations in `src/DirectoryService.Infrastructure.Postgres/Migrations` and include them in the same change as the model change.
- Preserve the `DirectoryServiceDbContext` connection-string key. Local development currently reads it from `src/DirectoryService.Web/appsettings.Development.json`; use environment-specific configuration for real credentials and never commit secrets.

From `backend/DirectoryService`, typical EF commands are:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/DirectoryService.Infrastructure.Postgres \
  --startup-project src/DirectoryService.Web \
  --output-dir Migrations

dotnet ef database update \
  --project src/DirectoryService.Infrastructure.Postgres \
  --startup-project src/DirectoryService.Web
```

Only run `database update` against an intentionally selected database. A migration can change shared or persistent data and must be reviewed before execution.

## Build, run, and verify

Run commands from `backend/DirectoryService` unless noted otherwise:

```bash
dotnet restore DirectoryService.slnx
dotnet build DirectoryService.slnx --configuration Debug
dotnet run --project src/DirectoryService.Web
```

Before handing off a change:

- Build the solution; warnings are errors through `backend/Directory.Build.props`.
- Add or update automated tests when a test project exists or when introducing non-trivial behavior. There is currently no checked-in test project for DirectoryService, so at minimum verify the build and exercise affected endpoints with a local PostgreSQL instance.
- Check `GET /api/health` and the relevant endpoint responses, including validation, not-found, conflict, and persistence-failure paths where applicable.
- For migrations, inspect the generated files and confirm the model snapshot is updated.
- Review `git diff` for accidental changes to `bin/`, `obj/`, IDE metadata, configuration secrets, or generated artifacts.

## Code style and implementation guardrails

- Follow the existing file-scoped namespaces, nullable reference types, implicit usings, records for DTOs/commands, and primary domain vocabulary.
- Keep async methods genuinely asynchronous; do not use `.Result`, `.Wait()`, or fire-and-forget work in request handlers.
- Always pass cancellation tokens to database and validation calls.
- Use source-generated `[LoggerMessage]` methods already used by handlers and infrastructure. Log identifiers and operation context, never passwords or full connection strings.
- Keep controllers focused on HTTP translation. Business rules belong in Core or Domain, and SQL/EF details belong in Infrastructure.
- Do not introduce a new mediator, result wrapper, mapping library, repository style, or DI pattern without a service-wide need and explicit justification.
- Preserve public routes, DTO shapes, error codes, and response envelopes unless the task explicitly changes the API contract.

## Useful paths

- Solution: `backend/DirectoryService/DirectoryService.slnx`
- Web entry point: `src/DirectoryService.Web/Program.cs`
- Controllers: `src/DirectoryService.Web/Controllers`
- Use cases: `src/DirectoryService.Core/Features`
- Domain model: `src/DirectoryService.Domain/Models` and `src/DirectoryService.Domain/ValueObjects`
- Database context: `src/DirectoryService.Infrastructure.Postgres/DirectoryServiceDbContext.cs`
- EF mappings and migrations: `src/DirectoryService.Infrastructure.Postgres/Configurations` and `src/DirectoryService.Infrastructure.Postgres/Migrations`
- Shared errors: `src/DirectoryService.Shared/Errors`
