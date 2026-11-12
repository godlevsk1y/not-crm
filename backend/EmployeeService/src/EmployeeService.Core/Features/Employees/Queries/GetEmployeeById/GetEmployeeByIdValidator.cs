using System.Text.Json;
using FluentValidation;

namespace EmployeeService.Core.Features.Employees.Queries.GetEmployeeById;

public class GetEmployeeByIdValidator : AbstractValidator<GetEmployeeByIdQuery>
{
    public GetEmployeeByIdValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage(JsonSerializer.Serialize(EmployeeErrors.IdEmpty()));
    }
}
