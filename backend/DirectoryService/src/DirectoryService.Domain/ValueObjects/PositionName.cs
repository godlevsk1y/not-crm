using CSharpFunctionalExtensions;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.ValueObjects;

public record PositionName
{
    public const int MaxLength = 100;
    
    public string Value { get; }

    private PositionName(string value)
    {
        Value = value;
    }

    public static Result<PositionName, Error> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValueObjectErrors.PositionName.Empty();
        }
        
        name = name.Trim();

        if (name.Length > MaxLength)
        {
            return ValueObjectErrors.PositionName.TooLong();
        }

        return new PositionName(name);
    }
}