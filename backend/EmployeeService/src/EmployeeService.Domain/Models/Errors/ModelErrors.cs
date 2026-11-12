using Shared.Kernel.Errors;

namespace EmployeeService.Domain.Models.Errors;

public static class ModelErrors
{
    public static class Employee
    {
        public static Error NameEmpty() => Error.Domain(new ErrorMessage(
            "employee.name.empty", "Employee name cannot be empty"));

        public static Error PositionEmpty() => Error.Domain(new ErrorMessage(
            "employee.position.empty", "Position identifier cannot be empty"));

        public static Error DepartmentEmpty() => Error.Domain(new ErrorMessage(
            "employee.department.empty", "Department identifier cannot be empty"));

        public static Error Deleted() => Error.Domain(new ErrorMessage(
            "employee.deleted", "Restore the employee before changing it"));

        public static Error AlreadyDismissed() => Error.Domain(new ErrorMessage(
            "employee.dismissed", "Employee is already dismissed"));

        public static Error DismissalBeforeHiring() => Error.Domain(new ErrorMessage(
            "employee.dismissal.before.hiring", "Dismissal date cannot precede hiring date"));
    }
}
