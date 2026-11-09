# EmployeeService

Service foundation targeting .NET 10 with the shared backend build settings and
central package versions. The storage project is a placeholder; startup requires
no database or other external services.

Run from the repository root:

```sh
dotnet restore backend/EmployeeService/EmployeeService.slnx
dotnet build backend/EmployeeService/EmployeeService.slnx --no-restore
dotnet test backend/EmployeeService/EmployeeService.slnx --no-build
dotnet run --project backend/EmployeeService/src/EmployeeService.Web -- --environment Development --urls http://localhost:5100
```

- Liveness: `http://localhost:5100/health/live`
- OpenAPI (Development): `http://localhost:5100/openapi/v1.json`
- Scalar (Development): `http://localhost:5100/scalar/v1`

Console logs include `ServiceName = EmployeeService`. HTTP exceptions use the
existing `GodlevskiyDev.Shared.Framework` middleware.
