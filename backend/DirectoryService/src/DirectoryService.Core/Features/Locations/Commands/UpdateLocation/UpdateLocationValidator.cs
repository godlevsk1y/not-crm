using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;

namespace DirectoryService.Core.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationValidator()
    {
        RuleFor(request => request.Name!)
            .MustBeValueObject(LocationName.Create)
            .When(request => request.Name is not null);
    }
}