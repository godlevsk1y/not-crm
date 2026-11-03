# DirectoryService logging

Application events and HTTP summaries use the same Serilog configuration and sinks
(console and Seq). Request events share an activity `TraceId`; a request identifier
is used as a fallback when no activity exists. `SourceName` is the short console
source; the full `SourceContext` and other diagnostic properties remain in Seq.
The HTTP middleware explicitly resolves the host's Serilog logger from DI, rather
than using the static bootstrap logger. Keeping the bootstrap logger separate
also allows integration-test hosts to run concurrently without sharing logger
configuration or lifetime.

The normal level is `Information`. Each request has one completion event with
`RequestMethod`, `RequestPath`, `StatusCode`, and `Elapsed`. Query strings, request
bodies, headers, credentials, and connection strings are not added to these events.
Successful `/health/live` and `/health/ready` requests use `Debug`; failing checks
remain visible. Database cleanup logs empty runs at `Debug` and actual deletions
at `Information`, with `RowsAffected`.

## Exception ownership

- Transaction boundaries log unexpected failures before converting them to an
  internal error result. Recognized database conflicts and missing references
  return their expected errors without an exception event.
- `ExceptionMiddleware` logs unexpected exceptions that escape the application.
  The HTTP completion event records the outcome without repeating the stack.
- If a response has already started, the middleware logs once and aborts it. The
  HTTP event retains the status already sent to the client and includes
  `RequestFailed=true` and `ResponseStarted=true`, at `Error` level.
- An `OperationCanceledException` with a cancelled request token is a client
  cancellation. Before the response starts it produces status 499; an existing
  response is left intact. The summary includes `RequestAborted=true`, without an
  exception stack. Cancellation without a cancelled request token is unexpected.

DirectoryService consumes `GodlevskiyDev.Shared.Framework` version `1.0.2` through
NuGet. This version includes the middleware changes above. The library's sources
remain in `backend/Shared`; DirectoryService and its Docker build do not reference
or copy the library project.

## Enabling diagnostics temporarily

EF Core and Npgsql logging is set to `Fatal` by default because they can log the
same database exception before the application maps or records it. Application
transaction logs and failing health checks still report unexpected failures.
Enable selected categories explicitly when investigating a database problem, for
example in a local settings override:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "Npgsql.Command": "Debug",
        "Serilog.AspNetCore.RequestLoggingMiddleware": "Debug",
        "DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup": "Debug"
      }
    }
  }
}
```

Enabling driver diagnostics can reintroduce duplicate database exception events.
Keep EF sensitive-data logging and Npgsql parameter/error-detail logging disabled.
Configure the Seq sink using `Serilog__WriteTo__1__Args__serverUrl` and its readiness
check using `Seq__ServerUrl` when running outside the default local setup.

## Verification

```bash
dotnet build DirectoryService.slnx --configuration Debug --disable-build-servers -m:1
dotnet test DirectoryService.slnx --no-build --filter FullyQualifiedName~Logging
```

The logging integration tests use PostgreSQL in Docker and capture the configured
Serilog pipeline. They cover success and trace correlation, validation, missing
entities, real database conflicts and failures, unexpected request/transaction
exceptions, cancellation, responses that have already started, health probes,
department transfers, and console formatting.
