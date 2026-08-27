using CSharpFunctionalExtensions;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.ValueObjects;

public record LocationName
{
    public const int MaxLength = 100;
    
    public string Value { get; }

    private LocationName(string value)
    {
        Value = value;
    }

    public static Result<LocationName, Error> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValueObjectErrors.LocationName.Empty();
        }
        
        name = name.Trim();

        if (name.Length > MaxLength)
        {
            return ValueObjectErrors.LocationName.TooLong();
        }

        return new LocationName(name);
    }

    public override string ToString() => Value;
}