using DirectoryService.Contracts.WebApi.Departments;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Validators;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(d => d.Name)
            .MaximumLength(100)
            .WithErrorCode("department.name.too.long")
            .WithMessage("Name must not exceed 100 characters");
        
        RuleFor(d => d.Slug)
            .MaximumLength(100)
            .WithErrorCode("department.slug.too.long")
            .WithMessage("Slug must not exceed 100 characters");
    }
}