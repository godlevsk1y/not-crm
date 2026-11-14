using EmployeeService.Core.Validation;
using EmployeeService.Domain.ValueObjects;
using FluentValidation;
using Shared.Kernel.Errors;

namespace EmployeeService.Core.Features.Employees.Commands.HireEmployee;

public class HireEmployeeCommandValidator : AbstractValidator<HireEmployeeCommand>
{
    public HireEmployeeCommandValidator()
    {
        RuleFor(command => command)
            .MustBeValueObject(command => 
                EmployeeName.Create(
                    command.FirstName, 
                    command.LastName));
        
        RuleFor(command => command.PositionId)
            .NotEmpty()
            .WithError(Error.Validation(
                new ErrorMessage(
                    "employee.position.id.invalid", 
                    "PositionId cannot be empty",
                    nameof(HireEmployeeCommand.PositionId))));
        
        RuleFor(command => command.DepartmentId)
            .NotEmpty()
            .WithError(Error.Validation(
                new ErrorMessage(
                    "employee.department.id.invalid", 
                    "Department cannot be empty",
                    nameof(HireEmployeeCommand.DepartmentId))));
    }
}