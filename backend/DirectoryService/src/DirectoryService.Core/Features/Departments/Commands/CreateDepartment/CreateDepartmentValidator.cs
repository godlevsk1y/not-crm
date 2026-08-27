using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Commands.CreateDepartment;

public class CreateDepartmentValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentValidator()
    {
        RuleFor(request => request.Name)
            .MustBeValueObject(DepartmentName.Create);
        
        RuleFor(request => request.Slug)
            .MustBeValueObject(Slug.Create);

        RuleFor(request => request.LocationIds)
            .NotNull()
            .WithError(Error.Validation(
                new ErrorMessage(
                    "department.locations.null", 
                    "Location IDs cannot be null", 
                    nameof(CreateDepartmentRequest.LocationIds)
                )
            ));
    }
}