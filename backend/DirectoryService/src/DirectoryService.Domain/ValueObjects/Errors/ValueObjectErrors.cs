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
        public static Error CountryEmpty() =>
            Error.Domain(new ErrorMessage("address.country.empty", "Country cannot be empty"));
        
        public static Error CityEmpty() =>
            Error.Domain(new ErrorMessage("address.city.empty", "City cannot be empty"));
        
        public static Error StreetEmpty() =>
            Error.Domain(new ErrorMessage("address.street.empty", "Street cannot be empty"));
        
        public static Error HouseNumberEmpty() =>
            Error.Domain(new ErrorMessage("address.housenumber.empty", "House number cannot be empty"));
    }

    public static class LocationName
    {
        public static Error Empty() => 
            Error.Domain(new ErrorMessage("location.name.empty", "Location name cannot be empty"));
        
        public static Error TooLong() =>
            Error.Domain(new ErrorMessage("location.name.long", "Location name is too long"));
    }
    
    public static class PositionName
    {
        public static Error Empty() => 
            Error.Domain(new ErrorMessage("position.name.empty", "Position name cannot be empty"));
        
        public static Error TooLong() =>
            Error.Domain(new ErrorMessage("position.name.long", "Position name is too long"));
    }
    
    public static class DepartmentName
    {
        public static Error Empty() => 
            Error.Domain(new ErrorMessage("department.name.empty", "Department name cannot be empty"));
        
        public static Error TooLong() =>
            Error.Domain(new ErrorMessage("department.name.long", "Department name is too long"));
    }
}