using DirectoryService.Contracts.WebApi.Locations;
using FluentValidation;

namespace DirectoryService.Core.Features.Locations.Validators;

public class UpdateLocationValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100)
            .WithErrorCode("location.name.too.long")
            .WithMessage("Name must not exceed 100 characters");
        
        RuleFor(x => x.Country)
            .MaximumLength(100)
            .WithErrorCode("location.country.too.long")
            .WithMessage("Country must not exceed 100 characters");
        
        RuleFor(x => x.Region)
            .MaximumLength(100)
            .WithErrorCode("location.region.too.long")
            .WithMessage("Region must not exceed 100 characters");
        
        RuleFor(x => x.City)
            .MaximumLength(100)
            .WithErrorCode("location.city.too.long")
            .WithMessage("City must not exceed 100 characters");

        RuleFor(x => x.District)
            .MaximumLength(60)
            .WithErrorCode("location.district.too.long")
            .WithMessage("District must not exceed 60 characters");
        
        RuleFor(x => x.Street)
            .MaximumLength(100)
            .WithErrorCode("location.street.too.long")
            .WithMessage("Street must not exceed 100 characters");

        RuleFor(x => x.HouseNumber)
            .MaximumLength(20)
            .WithErrorCode("location.housenumber.too.long")
            .WithMessage("House number must not exceed 20 characters")
            .Matches(@"^\d+[a-яА-Яa-zA-Z]*(?:\/[\d[a-яА-Яa-zA-Z]+)?(?:(?:\s|к|корп|стр)\.?\s?\d+[a-яА-Я]*)?$")
            .WithErrorCode("location.housenumber.invalid")
            .WithMessage("House number is invalid");

        RuleFor(x => x.PostalCode)
            .Matches(@"^\d{6}$")
            .WithErrorCode("location.postalcode.too.long")
            .WithMessage("Postal code must contain 6 digits");
    }
}