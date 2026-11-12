using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using EmployeeService.Domain.ValueObjects;
using Shared.Kernel.Errors;

namespace EmployeeService.UnitTests.Domain.Models;

public class EmployeeTests
{
    private static readonly DateOnly HiringDate = new(2026, 1, 1);

    [Fact]
    public void CreateInitializesEmployee()
    {
        var name = EmployeeName.Create("Ada", "Lovelace").Value;
        var position = new PositionId(Guid.NewGuid());
        var department = new DepartmentId(Guid.NewGuid());
        var before = DateTime.UtcNow;

        var result = Employee.Create(name, position, department, HiringDate);

        result.IsSuccess.Should().BeTrue();
        var employee = result.Value;

        employee.Id.Value.Should().NotBe(Guid.Empty);
        employee.Name.FirstName.Should().Be("Ada");
        employee.Name.LastName.Should().Be("Lovelace");
        employee.HiredAt.Should().Be(HiringDate);
        employee.PositionId.Should().Be(position);
        employee.DepartmentId.Should().Be(department);
        employee.DismissedAt.Should().BeNull();
        employee.DeletedAt.Should().BeNull();
        employee.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
        employee.UpdatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void CreateAllowsDuplicateNamesAndGeneratesDistinctIds()
    {
        var first = CreateEmployee();

        var second = Employee.Create(first.Name, first.PositionId, first.DepartmentId, HiringDate);

        second.IsSuccess.Should().BeTrue();
        second.Value.Name.Should().Be(first.Name);
        second.Value.Id.Should().NotBe(first.Id);
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

        creation.IsFailure.Should().BeTrue();
        AssertError(creation.Error, code);
        change.IsFailure.Should().BeTrue();
        AssertError(change.Error, code);
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void NullNameIsRejectedWithoutChangingState()
    {
        var employee = CreateEmployee();
        var state = Snapshot(employee);
        var creation = Employee.Create(null, employee.PositionId, employee.DepartmentId, HiringDate);
        var rename = employee.Rename(null);

        creation.IsFailure.Should().BeTrue();
        AssertError(creation.Error, "employee.name.empty");
        rename.IsFailure.Should().BeTrue();
        AssertError(rename.Error, "employee.name.empty");
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void RenameUpdatesNameAndTimestamp()
    {
        var employee = CreateEmployee();
        var name = EmployeeName.Create("Grace", "Hopper").Value;
        var before = WaitForNextInstant(employee);

        employee.Rename(name).IsSuccess.Should().BeTrue();

        employee.Name.Should().Be(name);
        AssertUpdated(employee, before);
    }

    [Fact]
    public void ChangeAssignmentUpdatesBothIdentifiersAndTimestamp()
    {
        var employee = CreateEmployee();
        var position = new PositionId(Guid.NewGuid());
        var department = new DepartmentId(Guid.NewGuid());
        var before = WaitForNextInstant(employee);

        employee.ChangeAssignment(position, department).IsSuccess.Should().BeTrue();

        employee.PositionId.Should().Be(position);
        employee.DepartmentId.Should().Be(department);
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

        employee.Dismiss(date).IsSuccess.Should().BeTrue();

        employee.DismissedAt.Should().Be(date);
        employee.DeletedAt.Should().BeNull();
        AssertUpdated(employee, before);
    }

    [Fact]
    public void DismissWhenAlreadyDismissedReturnsErrorAndPreservesState()
    {
        var employee = CreateEmployee();
        employee.Dismiss(HiringDate);
        var state = Snapshot(employee);

        var result = employee.Dismiss(HiringDate.AddDays(1));

        result.IsFailure.Should().BeTrue();
        AssertError(result.Error, "employee.dismissed");
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void DismissBeforeHiringPreservesState()
    {
        var employee = CreateEmployee();
        var state = Snapshot(employee);

        var result = employee.Dismiss(HiringDate.AddDays(-1));

        result.IsFailure.Should().BeTrue();
        AssertError(result.Error, "employee.dismissal.before.hiring");
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void SoftDeleteMarksEmployeeAsDeletedAndUpdatesTimestamp()
    {
        var employee = CreateEmployee();
        var before = WaitForNextInstant(employee);

        employee.SoftDelete();

        employee.DeletedAt.Should().Be(employee.UpdatedAt);
        employee.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
        AssertUpdated(employee, before);
    }

    [Fact]
    public void SoftDeleteWhenAlreadyDeletedDoesNotChangeState()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        var state = Snapshot(employee);
        WaitForNextInstant(employee);

        employee.SoftDelete();

        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void RestoreClearsDeletionAndUpdatesTimestamp()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        var before = WaitForNextInstant(employee);

        employee.Restore();

        employee.DeletedAt.Should().BeNull();
        AssertUpdated(employee, before);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RestoreWhenNotDeletedDoesNotChangeState(bool previouslyDeleted)
    {
        var employee = CreateEmployee();
        if (previouslyDeleted)
        {
            employee.SoftDelete();
            employee.Restore();
        }

        var state = Snapshot(employee);
        WaitForNextInstant(employee);

        employee.Restore();

        Snapshot(employee).Should().Be(state);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SoftDeleteAndRestorePreserveEmployment(bool dismissed)
    {
        var employee = CreateEmployee();
        if (dismissed)
        {
            employee.Dismiss(HiringDate.AddDays(20));
        }

        var original = Snapshot(employee);

        employee.SoftDelete();

        Snapshot(employee).Should().Be(original with
        {
            UpdatedAt = employee.UpdatedAt,
            DeletedAt = employee.DeletedAt,
        });

        employee.Restore();

        Snapshot(employee).Should().Be(original with { UpdatedAt = employee.UpdatedAt });
    }

    [Fact]
    public void RenameWhenDeletedReturnsErrorAndPreservesState()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        var state = Snapshot(employee);
        var name = EmployeeName.Create("Grace", "Hopper").Value;

        var result = employee.Rename(name);

        result.IsFailure.Should().BeTrue();
        AssertError(result.Error, "employee.deleted");
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void ChangeAssignmentWhenDeletedReturnsErrorAndPreservesState()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        var state = Snapshot(employee);
        var position = new PositionId(Guid.NewGuid());
        var department = new DepartmentId(Guid.NewGuid());

        var result = employee.ChangeAssignment(position, department);

        result.IsFailure.Should().BeTrue();
        AssertError(result.Error, "employee.deleted");
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void DismissWhenDeletedReturnsErrorAndPreservesState()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        var state = Snapshot(employee);

        var result = employee.Dismiss(HiringDate);

        result.IsFailure.Should().BeTrue();
        AssertError(result.Error, "employee.deleted");
        Snapshot(employee).Should().Be(state);
    }

    [Fact]
    public void RestoredEmployeeCanBeRenamedReassignedAndDismissed()
    {
        var employee = CreateEmployee();
        employee.SoftDelete();
        employee.Restore();
        var name = EmployeeName.Create("Grace", "Hopper").Value;
        var position = new PositionId(Guid.NewGuid());
        var department = new DepartmentId(Guid.NewGuid());

        var rename = employee.Rename(name);
        var assignment = employee.ChangeAssignment(position, department);
        var dismissal = employee.Dismiss(HiringDate);

        rename.IsSuccess.Should().BeTrue();
        assignment.IsSuccess.Should().BeTrue();
        dismissal.IsSuccess.Should().BeTrue();
        employee.Name.Should().Be(name);
        employee.PositionId.Should().Be(position);
        employee.DepartmentId.Should().Be(department);
        employee.DismissedAt.Should().Be(HiringDate);
    }

    private static Employee CreateEmployee() => Employee.Create(
        EmployeeName.Create("Ada", "Lovelace").Value,
        new PositionId(Guid.NewGuid()), new DepartmentId(Guid.NewGuid()), HiringDate).Value;

    private static void AssertError(Error error, string code)
    {
        error.Type.Should().Be(ErrorType.Domain);
        error.Messages.Should().ContainSingle().Which.Code.Should().Be(code);
    }

    private static DateTime WaitForNextInstant(Employee employee)
    {
        var clockAdvanced = SpinWait.SpinUntil(
            () => DateTime.UtcNow > employee.UpdatedAt, TimeSpan.FromSeconds(1));
        clockAdvanced.Should().BeTrue("the UTC clock must advance before checking timestamp changes");
        return employee.UpdatedAt;
    }

    private static void AssertUpdated(Employee employee, DateTime before)
    {
        employee.UpdatedAt.Should().BeAfter(before);
        employee.UpdatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
        employee.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    private static EmployeeState Snapshot(Employee employee) => new(
        employee.Id, employee.Name, employee.PositionId, employee.DepartmentId,
        employee.HiredAt, employee.DismissedAt, employee.UpdatedAt, employee.DeletedAt);

    private sealed record EmployeeState(
        EmployeeId Id, EmployeeName Name, PositionId PositionId, DepartmentId DepartmentId,
        DateOnly HiredAt, DateOnly? DismissedAt, DateTime UpdatedAt, DateTime? DeletedAt);
}
