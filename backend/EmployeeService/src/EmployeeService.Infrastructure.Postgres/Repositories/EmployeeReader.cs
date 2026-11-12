using Dapper;
using EmployeeService.Contracts.Employees;
using EmployeeService.Core.Database;
using Npgsql;

namespace EmployeeService.Infrastructure.Postgres.Repositories;

public class EmployeeReader : IEmployeeReader
{
    private readonly NpgsqlDataSource _dataSource;

    public EmployeeReader(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT id, first_name, last_name, position_id, department_id,
                                  hired_at, dismissed_at, updated_at
                           FROM employees
                           WHERE id = @id AND deleted_at IS NULL
                           """;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(sql, new { id }, cancellationToken: cancellationToken));

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        // Map the selected columns explicitly, preserving PostgreSQL date and UUID types.
        return new EmployeeDto(
            Id: reader.GetGuid(0),
            FirstName: reader.GetString(1),
            LastName: reader.GetString(2),
            PositionId: reader.GetGuid(3),
            DepartmentId: reader.GetGuid(4),
            HiredAt: await reader.GetFieldValueAsync<DateOnly>(5, cancellationToken),
            DismissedAt: await reader.IsDBNullAsync(6, cancellationToken)
                ? null
                : await reader.GetFieldValueAsync<DateOnly>(6, cancellationToken),
            UpdatedAt: reader.GetDateTime(7));
    }
}
