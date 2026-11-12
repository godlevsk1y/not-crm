using CSharpFunctionalExtensions;
using EmployeeService.Domain.ValueObjects.Errors;
using Shared.Kernel.Errors;

namespace EmployeeService.Domain.ValueObjects;

public record EmployeeName
{
    public string FirstName { get; }

    public string LastName { get; }

    private EmployeeName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static Result<EmployeeName, Error> Create(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return ValueObjectErrors.EmployeeName.FirstNameEmpty();
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return ValueObjectErrors.EmployeeName.LastNameEmpty();
        }

        return new EmployeeName(firstName.Trim(), lastName.Trim());
    }
}
