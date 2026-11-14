using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace EmployeeService.Contracts.Employees;

public sealed record EmployeeDto
{
    [Description("Employee UUID.")]
    public required Guid Id { get; init; }

    [Description("Employee first name.")]
    public required string FirstName { get; init; }

    [Description("Employee last name.")]
    public required string LastName { get; init; }

    [Description("Position UUID; no directory lookup is performed.")]
    public required Guid PositionId { get; init; }

    [Description("Department UUID; no directory lookup is performed.")]
    public required Guid DepartmentId { get; init; }

    [Description("Hiring date in yyyy-MM-dd format.")]
    public required DateOnly HiredAt { get; init; }

    [Description("Dismissal date in yyyy-MM-dd format; null if not dismissed.")]
    public required DateOnly? DismissedAt { get; init; }

    [Description("Last modification time in UTC.")]
    public required DateTime UpdatedAt { get; init; }

    public EmployeeDto() { }

    [SetsRequiredMembers]
    public EmployeeDto(
        Guid id,
        string firstName,
        string lastName,
        Guid positionId,
        Guid departmentId,
        DateOnly hiredAt,
        DateOnly? dismissedAt,
        DateTime updatedAt)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        PositionId = positionId;
        DepartmentId = departmentId;
        HiredAt = hiredAt;
        DismissedAt = dismissedAt;
        UpdatedAt = updatedAt;
    }
}
