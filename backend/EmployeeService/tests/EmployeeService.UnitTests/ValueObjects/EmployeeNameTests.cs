using EmployeeService.Domain.ValueObjects;
using Shared.Kernel.Errors;

namespace EmployeeService.UnitTests.ValueObjects;

public class EmployeeNameTests
{
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

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Domain);
        result.Error.Messages.Should().ContainSingle().Which.Code.Should().Be(code);
    }

    [Fact]
    public void CreateTrimsBothNameParts()
    {
        var result = EmployeeName.Create("  Ada\t", "\nLovelace  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Ada");
        result.Value.LastName.Should().Be("Lovelace");
    }

    [Fact]
    public void NamesWithSamePartsAreEqual()
    {
        var first = EmployeeName.Create("Ada", "Lovelace").Value;
        var second = EmployeeName.Create("Ada", "Lovelace").Value;

        first.Should().Be(second);
    }

    [Theory]
    [InlineData("Grace", "Lovelace")]
    [InlineData("Ada", "Hopper")]
    public void NamesWithDifferentPartsAreNotEqual(string firstName, string lastName)
    {
        var original = EmployeeName.Create("Ada", "Lovelace").Value;
        var different = EmployeeName.Create(firstName, lastName).Value;

        original.Should().NotBe(different);
    }
}
