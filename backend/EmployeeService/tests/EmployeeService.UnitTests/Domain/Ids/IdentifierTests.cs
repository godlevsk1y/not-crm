using EmployeeService.Domain.Ids;

namespace EmployeeService.UnitTests.Domain.Ids;

public class IdentifierTests
{
    [Fact]
    public void EmployeeId_ShouldPreserveValueAndSupportGuidConversion()
    {
        var value = Guid.NewGuid();
        var id = new EmployeeId(value);

        Guid converted = id;

        id.Value.Should().Be(value);
        id.ToGuid().Should().Be(value);
        converted.Should().Be(value);
        id.Should().Be(new EmployeeId(value));
        id.Should().NotBe(new EmployeeId(Guid.NewGuid()));
    }

    [Fact]
    public void PositionId_ShouldPreserveValueAndSupportGuidConversion()
    {
        var value = Guid.NewGuid();
        var id = new PositionId(value);

        Guid converted = id;

        id.Value.Should().Be(value);
        id.ToGuid().Should().Be(value);
        converted.Should().Be(value);
        id.Should().Be(new PositionId(value));
        id.Should().NotBe(new PositionId(Guid.NewGuid()));
    }

    [Fact]
    public void DepartmentId_ShouldPreserveValueAndSupportGuidConversion()
    {
        var value = Guid.NewGuid();
        var id = new DepartmentId(value);

        Guid converted = id;

        id.Value.Should().Be(value);
        id.ToGuid().Should().Be(value);
        converted.Should().Be(value);
        id.Should().Be(new DepartmentId(value));
        id.Should().NotBe(new DepartmentId(Guid.NewGuid()));
    }
}
