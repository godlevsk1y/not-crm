using DirectoryService.Contracts.WebApi.Locations;
using FluentValidation;

namespace DirectoryService.Core.Features.Locations.Validators;

public class CreateLocationValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
                .WithErrorCode("location.name.empty")
                .WithMessage("Name must not be empty")
            .MaximumLength(100)
                .WithErrorCode("location.name.too.long")
                .WithMessage("Name must not exceed 100 characters");
        
        RuleFor(x => x.Country)
            .NotEmpty()
                .WithErrorCode("location.country.empty")
                .WithMessage("Country must not be empty")
            .MaximumLength(100)
                .WithErrorCode("location.country.too.long")
                .WithMessage("Country must not exceed 100 characters");
        
        RuleFor(x => x.Region)
            .NotEmpty()
                .WithErrorCode("location.region.empty")
                .WithMessage("Region must not be empty")
            .MaximumLength(100)
                .WithErrorCode("location.region.too.long")
                .WithMessage("Region must not exceed 100 characters")
            .When(x => x.Region is not null);
        
        RuleFor(x => x.City)
            .NotEmpty()
                .WithErrorCode("location.city.empty")
                .WithMessage("City must not be empty")
            .MaximumLength(100)
                .WithErrorCode("location.city.too.long")
                .WithMessage("City must not exceed 100 characters");
        
        RuleFor(x => x.District)
            .NotEmpty()
                .WithErrorCode("location.district.empty")
                .WithMessage("District must not be empty")
            .MaximumLength(60)
                .WithErrorCode("location.district.too.long")
                .WithMessage("District must not exceed 60 characters")
            .When(x => x.District is not null);
        
        RuleFor(x => x.Street)
            .NotEmpty()
                .WithErrorCode("location.street.empty")
                .WithMessage("Street must not be empty")
            .MaximumLength(100)
                .WithErrorCode("location.street.too.long")
                .WithMessage("Street must not exceed 100 characters");
        
        RuleFor(x => x.HouseNumber)
            .NotEmpty()
                .WithErrorCode("location.housenumber.empty")
                .WithMessage("House number must not be empty")
            .MaximumLength(20)
                .WithErrorCode("location.housenumber.too.long")
                .WithMessage("House number must not exceed 20 characters")
            .Matches(@"^\d+[a-яА-Яa-zA-Z]*(?:\/[\d[a-яА-Яa-zA-Z]+)?(?:(?:\s|к|корп|стр)\.?\s?\d+[a-яА-Я]*)?$")
                .WithErrorCode("location.housenumber.invalid")
                .WithMessage("House number is invalid");
        
        RuleFor(x => x.PostalCode)
            .NotEmpty()
                .WithErrorCode("location.postalcode.empty")
                .WithMessage("Postal code must not be empty")
            .Matches(@"^\d{6}$")
                .WithErrorCode("location.postalcode.too.long")
                .WithMessage("Postal code must contain 6 digits")
            .When(x => x.PostalCode is not null);
    }
}