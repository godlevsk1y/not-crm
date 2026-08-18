using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.ValueObjects;
using FluentValidation;

namespace DirectoryService.Core.Features.Locations.Commands.CreateLocation;

public class CreateLocationValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationValidator()
    {
        RuleFor(request => request.Name)
            .MustBeValueObject(LocationName.Create);

        RuleFor(request => request)
            .MustBeValueObject(request => Address.Create(
                request.Country,
                request.Region,
                request.City,
                request.District,
                request.Street,
                request.HouseNumber,
                request.PostalCode
            ));
    }
}