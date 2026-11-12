using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using EmployeeService.Domain.ValueObjects;
using Shared.Kernel.Errors;

namespace EmployeeService.Domain.Tests;

public class EmployeeTests
{
    private static readonly DateOnly HiringDate = new(2026, 1, 1);

    [Fact]
    public void CreateInitializesEmployee()
    {
        var before = DateTime.UtcNow;
        var employee = CreateEmployee();
        var other = CreateEmployee();

        Assert.NotEqual(Guid.Empty, employee.Id.Value);
        Assert.NotEqual(employee.Id, other.Id);
        Assert.Equal("Ada", employee.Name.FirstName);
        Assert.Equal("Lovelace", employee.Name.LastName);
        Assert.Equal(HiringDate, employee.HiredAt);
        Assert.NotEqual(Guid.Empty, employee.PositionId.Value);
        Assert.NotEqual(Guid.Empty, employee.DepartmentId.Value);
        Assert.Null(employee.DismissedAt);
        Assert.Null(employee.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, employee.UpdatedAt.Kind);
        Assert.InRange(employee.UpdatedAt, before, DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null, "Last", "employee.name.first.empty")]
    [InlineData("", "Last", "employee.name.first.empty")]
    [InlineData(" \t\n", "Last", "employee.name.first.empty")]
    [InlineData("First", null, "employee.name.last.empty")]
    [InlineData("First", "", "employee.name.last.empty")]
    [InlineData("First", " \t\n", "employee.name.last.empty")]
    public void NameRejectsMissingParts(string? first, string? last, string code)
    {
        var result = EmployeeName.Create(first, last);

        Assert.True(result.IsFailure);
        AssertError(result.Error, code);
    }

    [Fact]
    public void NameTrimsBothPartsAndHasValueEquality()
    {
        var name = EmployeeName.Create("  Ada\t", "\nLovelace  ").Value;

        Assert.Equal(EmployeeName.Create("Ada", "Lovelace").Value, name);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void InvalidAssignmentIsRejectedWithoutChangingState(bool invalidPosition, bool nullId)
    {
        var employee = CreateEmployee();
        var state = Snapshot(employee);
        PositionId? emptyPosition = nullId ? null : new PositionId(Guid.Empty);
        DepartmentId? emptyDepartment = nullId ? null : new DepartmentId(Guid.Empty);
        PositionId? position = invalidPosition ? emptyPosition : new PositionId(Guid.NewGuid());
        DepartmentId? department = invalidPosition ? new DepartmentId(Guid.NewGuid()) : emptyDepartment;
        var code = invalidPosition ? "employee.position.empty" : "employee.department.empty";

        var creation = Employee.Create(employee.Name, position, department, HiringDate);
        var change = employee.ChangeAssignment(position, department);

        Assert.True(creation.IsFailure);
        AssertError(creation.Error, code);
        Assert.True(change.IsFailure);
        AssertError(change.Error, code);
        Assert.Equal(state, Snapshot(employee));
    }

    [Fact]
    public void NullNameIsRejectedWithoutChangingState()
    {
        var employee = CreateEmployee();
        var state = Snapshot(employee);
        var creation = Employee.Create(null, employee.PositionId, employee.DepartmentId, HiringDate);
        var rename = employee.Rename(null);

        Assert.True(creation.IsFailure);
        AssertError(creation.Error, "employee.name.empty");
        Assert.True(rename.IsFailure);
        AssertError(rename.Error, "employee.name.empty");
        Assert.Equal(state, Snapshot(employee));
    }

    [Fact]
    public void RenameUpdatesNameAndTimestamp()
    {
        var employee = CreateEmployee();
        var name = EmployeeName.Create("Grace", "Hopper").Value;
        var before = WaitForNextInstant(employee);

        Assert.True(employee.Rename(name).IsSuccess);

        Assert.Equal(name, employee.Name);
        AssertUpdated(employee, before);
    }

    [Fact]
    public void ChangeAssignmentUpdatesBothIdentifiersAndTimestamp()
    {
        var employee = CreateEmployee();
        var position = new PositionId(Guid.NewGuid());
        var department = new DepartmentId(Guid.NewGuid());
        var before = WaitForNextInstant(employee);

        Assert.True(employee.ChangeAssignment(position, department).IsSuccess);

        Assert.Equal(position, employee.PositionId);
        Assert.Equal(department, employee.DepartmentId);
        AssertUpdated(employee, before);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    public void DismissAcceptsHiringDateOrLater(int days)
    {
        var employee = CreateEmployee();
        var date = HiringDate.AddDays(days);
        var before = WaitForNextInstant(employee);

        Assert.True(employee.Dismiss(date).IsSuccess);

        Assert.Equal(date, employee.DismissedAt);
        Assert.Null(employee.DeletedAt);
        AssertUpdated(employee, before);
        var state = Snapshot(employee);
        var repeated = employee.Dismiss(date.AddDays(1));
        Assert.True(repeated.IsFailure);
        AssertError(repeated.Error, "employee.dismissed");
        Assert.Equal(state, Snapshot(employee));
    }

    [Fact]
    public void DismissBeforeHiringPreservesState()
    {
        var employee = CreateEmployee();
        var state = Snapshot(employee);

        var result = employee.Dismiss(HiringDate.AddDays(-1));

        Assert.True(result.IsFailure);
        AssertError(result.Error, "employee.dismissal.before.hiring");
        Assert.Equal(state, Snapshot(employee));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SoftDeleteAndRestorePreserveEmploymentAndAreIdempotent(bool dismissed)
    {
        var employee = CreateEmployee();
        if (dismissed)
        {
            Assert.True(employee.Dismiss(HiringDate.AddDays(20)).IsSuccess);
        }

        var original = Snapshot(employee);
        employee.Restore();
        Assert.Equal(original, Snapshot(employee));
        var before = WaitForNextInstant(employee);
        employee.SoftDelete();
        AssertUpdated(employee, before);
        Assert.Equal(employee.UpdatedAt, employee.DeletedAt);
        Assert.Equal(DateTimeKind.Utc, employee.DeletedAt!.Value.Kind);
        var deleted = Snapshot(employee);
        employee.SoftDelete();
        Assert.Equal(deleted, Snapshot(employee));
        before = WaitForNextInstant(employee);
        employee.Restore();
        AssertUpdated(employee, before);
        Assert.Null(employee.DeletedAt);
        Assert.Equal(HiringDate, employee.HiredAt);
        Assert.Equal(dismissed ? HiringDate.AddDays(20) : (DateOnly?)null, employee.DismissedAt);
        Assert.Equal(original with { UpdatedAt = employee.UpdatedAt }, Snapshot(employee));
        var restored = Snapshot(employee);
        employee.Restore();
        Assert.Equal(restored, Snapshot(employee));
    }

    [Fact]
    public void DeletedEmployeeRejectsMutationsUntilRestored()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        var state = Snapshot(employee);
        var name = EmployeeName.Create("Grace", "Hopper").Value;
        var rename = employee.Rename(name);
        var assignment = employee.ChangeAssignment(new PositionId(Guid.NewGuid()), new DepartmentId(Guid.NewGuid()));
        var dismissal = employee.Dismiss(HiringDate);

        Assert.True(rename.IsFailure);
        Assert.True(assignment.IsFailure);
        Assert.True(dismissal.IsFailure);
        AssertError(rename.Error, "employee.deleted");
        AssertError(assignment.Error, "employee.deleted");
        AssertError(dismissal.Error, "employee.deleted");
        Assert.Equal(state, Snapshot(employee));

        employee.Restore();
        Assert.True(employee.Rename(name).IsSuccess);
        Assert.True(employee.ChangeAssignment(new PositionId(Guid.NewGuid()), new DepartmentId(Guid.NewGuid())).IsSuccess);
        Assert.True(employee.Dismiss(HiringDate).IsSuccess);
    }

    private static Employee CreateEmployee() => Employee.Create(
        EmployeeName.Create("Ada", "Lovelace").Value,
        new PositionId(Guid.NewGuid()), new DepartmentId(Guid.NewGuid()), HiringDate).Value;

    private static void AssertError(Error error, string code)
    {
        Assert.Equal(ErrorType.Domain, error.Type);
        Assert.Equal(code, Assert.Single(error.Messages).Code);
    }

    private static DateTime WaitForNextInstant(Employee employee)
    {
        Assert.True(SpinWait.SpinUntil(() => DateTime.UtcNow > employee.UpdatedAt, TimeSpan.FromSeconds(1)));
        return employee.UpdatedAt;
    }

    private static void AssertUpdated(Employee employee, DateTime before)
    {
        Assert.True(employee.UpdatedAt > before);
        Assert.True(employee.UpdatedAt <= DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, employee.UpdatedAt.Kind);
    }

    private static EmployeeState Snapshot(Employee employee) => new(
        employee.Id, employee.Name, employee.PositionId, employee.DepartmentId,
        employee.HiredAt, employee.DismissedAt, employee.UpdatedAt, employee.DeletedAt);

    private sealed record EmployeeState(
        EmployeeId Id, EmployeeName Name, PositionId PositionId, DepartmentId DepartmentId,
        DateOnly HiredAt, DateOnly? DismissedAt, DateTime UpdatedAt, DateTime? DeletedAt);
}
