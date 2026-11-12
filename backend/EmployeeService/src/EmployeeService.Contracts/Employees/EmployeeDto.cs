using System.ComponentModel;

namespace EmployeeService.Contracts.Employees;

public record EmployeeDto(
    [property: Description("Employee UUID.")]
    Guid Id,
    [property: Description("Employee first name.")]
    string FirstName,
    [property: Description("Employee last name.")]
    string LastName,
    [property: Description("Position UUID; no directory lookup is performed.")]
    Guid PositionId,
    [property: Description("Department UUID; no directory lookup is performed.")]
    Guid DepartmentId,
    [property: Description("Hiring date in yyyy-MM-dd format.")]
    DateOnly HiredAt,
    [property: Description("Dismissal date in yyyy-MM-dd format; null if not dismissed.")]
    DateOnly? DismissedAt,
    [property: Description("Last modification time in UTC.")]
    DateTime UpdatedAt);
