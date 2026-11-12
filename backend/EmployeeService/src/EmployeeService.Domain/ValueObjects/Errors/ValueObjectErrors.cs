using Shared.Kernel.Errors;

namespace EmployeeService.Domain.ValueObjects.Errors;

public static class ValueObjectErrors
{
    public static class EmployeeName
    {
        public static Error FirstNameEmpty() => Error.Domain(new ErrorMessage(
            "employee.name.first.empty", "First name cannot be empty"));

        public static Error LastNameEmpty() => Error.Domain(new ErrorMessage(
            "employee.name.last.empty", "Last name cannot be empty"));
    }
}
