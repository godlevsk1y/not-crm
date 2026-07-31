using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Validators;

public class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentValidator()
    {
        RuleFor(request => request.Name!)
            .MustBeValueObject(DepartmentName.Create)
            .When(request => request.Name is not null);
        
        RuleFor(request => request.Slug!)
            .MustBeValueObject(Slug.Create)
            .When(request => request.Slug is not null);
    }
}