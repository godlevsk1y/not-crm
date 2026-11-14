using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Kernel.Errors;

namespace EmployeeService.Infrastructure.Postgres.Transactions;

internal static class PostgresExceptionMapper
{
    private const string EmployeePrimaryKeyConstraint = "pk_employees";

    internal static Error? Map(Exception exception)
    {
        var postgresException = GetPostgresException(exception);

        if (postgresException is null)
        {
            return null;
        }

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation =>
                MapUniqueConstraintViolation(postgresException),

            PostgresErrorCodes.DeadlockDetected =>
                Error.Conflict(new ErrorMessage(
                    "employee.save.conflict",
                    "A conflict occurred when saving an employee")),

            _ => null,
        };
    }

    private static Error? MapUniqueConstraintViolation(PostgresException exception) =>
        exception.ConstraintName switch
        {
            EmployeePrimaryKeyConstraint =>
                Error.Conflict(new ErrorMessage(
                    "employee.exists",
                    "Employee with this id already exists")),

            _ => null,
        };

    private static PostgresException? GetPostgresException(Exception exception)
    {
        return exception switch
        {
            PostgresException postgresException => postgresException,

            DbUpdateException
            {
                InnerException: PostgresException postgresException,
            } => postgresException,

            _ => null,
        };
    }
}
