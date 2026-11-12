using CSharpFunctionalExtensions;
using Dapper;
using EmployeeService.Contracts.Employees;
using EmployeeService.Core.Database;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;
using Shared.Kernel.Errors;

namespace EmployeeService.Core.Features.Employees.Queries.GetEmployeeById;

public class GetEmployeeByIdQueryHandler : IQueryHandler<GetEmployeeByIdQuery, Result<EmployeeDto, Error>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IValidator<GetEmployeeByIdQuery> _validator;

    public GetEmployeeByIdQueryHandler(IDbConnectionFactory connectionFactory, IValidator<GetEmployeeByIdQuery> validator)
    {
        _connectionFactory = connectionFactory;
        _validator = validator;
    }

    public async Task<Result<EmployeeDto, Error>> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        const string sql = """
                           SELECT id, first_name, last_name, position_id, department_id,
                                  hired_at, dismissed_at, updated_at
                           FROM employees
                           WHERE id = @id AND deleted_at IS NULL
                           """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var reader = await connection.ExecuteReaderAsync(
            new CommandDefinition(sql, new { id = query.Id }, cancellationToken: cancellationToken));

        if (!await reader.ReadAsync(cancellationToken))
        {
            return EmployeeErrors.NotFound(query.Id);
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
