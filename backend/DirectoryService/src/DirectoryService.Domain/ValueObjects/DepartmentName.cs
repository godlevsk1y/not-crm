using CSharpFunctionalExtensions;
using DirectoryService.Domain.ValueObjects.Errors;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.ValueObjects;

public record DepartmentName
{
    public const int MaxLength = 100;
    
    public string Value { get; }

    private DepartmentName(string value)
    {
        Value = value;
    }

    public static Result<DepartmentName, Error> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValueObjectErrors.DepartmentName.Empty();
        }
        
        name = name.Trim();

        if (name.Length > MaxLength)
        {
            return ValueObjectErrors.DepartmentName.TooLong();
        }

        return new DepartmentName(name);
    }
    
    public override string ToString() => Value;
}