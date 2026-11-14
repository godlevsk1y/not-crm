using System.ComponentModel;

namespace EmployeeService.Contracts.WebApi.Employees;

public record HireEmployeeRequest(
    [property: Description("Employee first name; must not be empty or whitespace. Leading and trailing whitespace is trimmed.")]
    string FirstName, 
    [property: Description("Employee last name; must not be empty or whitespace. Leading and trailing whitespace is trimmed.")]
    string LastName, 
    [property: Description("Non-empty UUID of an existing position in DirectoryService.")]
    Guid PositionId, 
    [property: Description("Non-empty UUID of an existing department in DirectoryService.")]
    Guid DepartmentId, 
    [property: Description("Hiring date in yyyy-MM-dd format.")]
    DateOnly HiredAt
);
