using CSharpFunctionalExtensions;
using EmployeeService.Contracts.Employees;
using EmployeeService.Core.Database;
using FluentValidation;
using Shared.Core.Abstractions;
using Shared.Core.Extensions;
using Shared.Kernel.Errors;

namespace EmployeeService.Core.Features.Employees.Queries.GetEmployeeById;

public class GetEmployeeByIdQueryHandler : IQueryHandler<GetEmployeeByIdQuery, Result<EmployeeDto, Error>>
{
    private readonly IEmployeeReader _employeeReader;
    private readonly IValidator<GetEmployeeByIdQuery> _validator;

    public GetEmployeeByIdQueryHandler(IEmployeeReader employeeReader, IValidator<GetEmployeeByIdQuery> validator)
    {
        _employeeReader = employeeReader;
        _validator = validator;
    }

    public async Task<Result<EmployeeDto, Error>> Handle(GetEmployeeByIdQuery query, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var employee = await _employeeReader.GetByIdAsync(query.Id, cancellationToken);
        if (employee is null)
        {
            return EmployeeErrors.NotFound(query.Id);
        }

        return employee;
    }
}
