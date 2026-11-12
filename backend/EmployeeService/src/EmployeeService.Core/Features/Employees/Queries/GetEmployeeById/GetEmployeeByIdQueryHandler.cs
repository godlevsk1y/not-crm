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
        var employee = await connection.QuerySingleOrDefaultAsync<EmployeeDto>(
            new CommandDefinition(sql, new { id = query.Id }, cancellationToken: cancellationToken));

        if (employee is null)
        {
            return EmployeeErrors.NotFound(query.Id);
        }

        return employee;
    }
}
