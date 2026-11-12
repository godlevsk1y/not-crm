using CSharpFunctionalExtensions;
using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models.Errors;
using EmployeeService.Domain.ValueObjects;
using Shared.Kernel.Errors;

namespace EmployeeService.Domain.Models;

public class Employee
{
    public EmployeeId Id { get; private set; } = null!;

    public EmployeeName Name { get; private set; } = null!;

    public PositionId PositionId { get; private set; } = null!;

    public DepartmentId DepartmentId { get; private set; } = null!;

    public DateOnly HiredAt { get; private set; }

    public DateOnly? DismissedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? DeletedAt { get; private set; }

    private Employee() { }

    public static Result<Employee, Error> Create(
        EmployeeName? name, PositionId? positionId, DepartmentId? departmentId, DateOnly hiredAt)
    {
        if (name is null)
        {
            return ModelErrors.Employee.NameEmpty();
        }

        var assignment = ValidateAssignment(positionId, departmentId);
        if (assignment.IsFailure)
        {
            return assignment.Error;
        }

        return new Employee
        {
            Id = new EmployeeId(Guid.NewGuid()),
            Name = name,
            PositionId = positionId!,
            DepartmentId = departmentId!,
            HiredAt = hiredAt,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public UnitResult<Error> Rename(EmployeeName? name)
    {
        if (DeletedAt is not null)
        {
            return ModelErrors.Employee.Deleted();
        }

        if (name is null)
        {
            return ModelErrors.Employee.NameEmpty();
        }

        Name = name;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ChangeAssignment(PositionId? positionId, DepartmentId? departmentId)
    {
        if (DeletedAt is not null)
        {
            return ModelErrors.Employee.Deleted();
        }

        var assignment = ValidateAssignment(positionId, departmentId);
        if (assignment.IsFailure)
        {
            return assignment;
        }

        PositionId = positionId!;
        DepartmentId = departmentId!;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Dismiss(DateOnly dismissedAt)
    {
        if (DeletedAt is not null)
        {
            return ModelErrors.Employee.Deleted();
        }

        if (DismissedAt is not null)
        {
            return ModelErrors.Employee.AlreadyDismissed();
        }

        if (dismissedAt < HiredAt)
        {
            return ModelErrors.Employee.DismissalBeforeHiring();
        }

        DismissedAt = dismissedAt;
        UpdatedAt = DateTime.UtcNow;
        return UnitResult.Success<Error>();
    }

    public void SoftDelete()
    {
        if (DeletedAt is not null)
        {
            return;
        }

        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DeletedAt.Value;
    }

    public void Restore()
    {
        if (DeletedAt is null)
        {
            return;
        }

        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private static UnitResult<Error> ValidateAssignment(PositionId? positionId, DepartmentId? departmentId)
    {
        if (positionId is null || positionId.Value == Guid.Empty)
        {
            return ModelErrors.Employee.PositionEmpty();
        }

        if (departmentId is null || departmentId.Value == Guid.Empty)
        {
            return ModelErrors.Employee.DepartmentEmpty();
        }

        return UnitResult.Success<Error>();
    }
}
