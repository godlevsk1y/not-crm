using Shared.Kernel.Errors;

namespace EmployeeService.Core.Features.Employees;

public static class EmployeeErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(new ErrorMessage("employee.not.found", $"Employee with id '{id}' was not found"));

    public static Error IdEmpty() =>
        Error.Validation(new ErrorMessage("employee.id.empty", "Employee id must not be empty", "Id"));
}
