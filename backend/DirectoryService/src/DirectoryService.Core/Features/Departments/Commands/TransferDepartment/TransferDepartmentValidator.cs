using DirectoryService.Core.Validation;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Commands.TransferDepartment;

public class TransferDepartmentValidator : AbstractValidator<TransferDepartmentCommand>
{
    public TransferDepartmentValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithError(Error.Validation(new ErrorMessage(
                "department.id.empty", 
                "Department Id cannot be empty",
                nameof(TransferDepartmentCommand.DepartmentId)
            )));
    }
}