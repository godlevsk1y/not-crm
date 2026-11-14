using System.Diagnostics.CodeAnalysis;
using Shared.Core.Abstractions;

namespace EmployeeService.Core.Features.Employees.Commands.HireEmployee;

public record HireEmployeeCommand : ICommand
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required Guid PositionId { get; init; }
    public required Guid DepartmentId { get; init; }
    public required DateOnly HiredAt { get; init; }

    public HireEmployeeCommand() { }
    
    [SetsRequiredMembers]
    public HireEmployeeCommand(
        string firstName, string lastName, Guid positionId, Guid departmentId, DateOnly hiredAt)
    {
        FirstName = firstName;
        LastName = lastName;
        PositionId = positionId;
        DepartmentId = departmentId;
        HiredAt = hiredAt;
    }
}