using DirectoryService.Contracts.WebApi.Departments;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Validators;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentValidator()
    {
        RuleFor(d => d.Name)
            .NotEmpty()
                .WithErrorCode("department.name.empty")
                .WithMessage("Name is required")
            .MaximumLength(100)
                .WithErrorCode("department.name.too.long")
                .WithMessage("Name must not exceed 100 characters");
        
        RuleFor(d => d.Slug)
            .NotEmpty()
                .WithErrorCode("department.slug.empty")
                .WithMessage("Slug is required")
            .MaximumLength(100)
                .WithErrorCode("department.slug.too.long")
                .WithMessage("Slug must not exceed 100 characters");

        RuleFor(d => d.LocationIds)
            .NotNull()
            .WithErrorCode("department.locations.null")
            .WithMessage("Location IDs cannot be null");
    }
}