using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.ValueObjects.Errors;

public static class ValueObjectErrors
{
    public static class Slug
    {
        public static Error Empty()
            => Error.Domain(new ErrorMessage("slug.empty", "Slug cannot be empty"));
        
        public static Error Invalid()
            => Error.Domain(new ErrorMessage("slug.invalid", "Slug is invalid"));
    }

    public static class Address
    {
        public static ErrorMessage CountryEmptyMessage =>
            new("address.country.empty", "Country cannot be empty", nameof(ValueObjects.Address.Country));
        
        public static ErrorMessage CityEmptyMessage =>
            new("address.city.empty", "City cannot be empty", nameof(ValueObjects.Address.City));
        
        public static ErrorMessage StreetEmptyMessage =>
            new("address.street.empty", "Street cannot be empty", nameof(ValueObjects.Address.Street));
        
        public static ErrorMessage HouseNumberEmptyMessage =>
            new("address.housenumber.empty", "House number cannot be empty", nameof(ValueObjects.Address.HouseNumber));
        
        public static Error Invalid(params IEnumerable<ErrorMessage> errors)
            => Error.Domain(errors);
    }

    public static class LocationName
    {
        public static Error Empty() => 
            Error.Domain(new ErrorMessage(
                "location.name.empty", 
                "Location name cannot be empty", 
                nameof(ValueObjects.LocationName)));
        
        public static Error TooLong() =>
            Error.Domain(new ErrorMessage(
                "location.name.long", 
                "Location name is too long", 
                nameof(ValueObjects.LocationName)));
    }
    
    public static class PositionName
    {
        public static Error Empty() => 
            Error.Domain(new ErrorMessage(
                "position.name.empty",
                "Position name cannot be empty",
                nameof(ValueObjects.PositionName)));
        
        public static Error TooLong() =>
            Error.Domain(new ErrorMessage(
                "position.name.long", 
                "Position name is too long",
                nameof(ValueObjects.PositionName)));
    }
    
    public static class DepartmentName
    {
        public static Error Empty() => 
            Error.Domain(new ErrorMessage(
                "department.name.empty", 
                "Department name cannot be empty",
                nameof(ValueObjects.DepartmentName)));
        
        public static Error TooLong() =>
            Error.Domain(new ErrorMessage(
                "department.name.long", 
                "Department name is too long",
                nameof(ValueObjects.DepartmentName)));
    }
}