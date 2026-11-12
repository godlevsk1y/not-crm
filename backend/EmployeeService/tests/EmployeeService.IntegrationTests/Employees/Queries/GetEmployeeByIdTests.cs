using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmployeeService.Contracts.Employees;
using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using EmployeeService.Domain.ValueObjects;
using EmployeeService.Infrastructure.Postgres;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;
using Shared.Kernel.Errors;

namespace EmployeeService.IntegrationTests.Employees.Queries;

// xUnit runs tests in this class sequentially, sharing one PostgreSQL container.
public class GetEmployeeByIdTests : IClassFixture<EmployeeServiceTestWebFactory>, IAsyncLifetime
{
    private readonly EmployeeServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public GetEmployeeByIdTests(EmployeeServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetById_ShouldReturnAllPersistedFields_WhenEmployeeExists()
    {
        // Arrange
        var employee = await SaveEmployeeAsync(CreateEmployee());
        await SaveEmployeeAsync(CreateEmployee());

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await ReadSuccessResultAsync(response);
        var card = result.Deserialize<EmployeeDto>(JsonSerializerOptions.Web);
        card.Should().NotBeNull();
        card!.Id.Should().Be(employee.Id.Value);
        card.FirstName.Should().Be(employee.Name.FirstName);
        card.LastName.Should().Be(employee.Name.LastName);
        card.PositionId.Should().Be(employee.PositionId.Value);
        card.DepartmentId.Should().Be(employee.DepartmentId.Value);
        card.HiredAt.Should().Be(employee.HiredAt);
        card.DismissedAt.Should().Be(employee.DismissedAt);
        card.UpdatedAt.Should().Be(employee.UpdatedAt);
        card.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetById_ShouldReturnDirectoryIdsWithoutNames_WhenDirectoryServiceIsUnavailable()
    {
        // Arrange: the factory starts only PostgreSQL; directory IDs are arbitrary UUIDs.
        var employee = await SaveEmployeeAsync(CreateEmployee());

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await ReadSuccessResultAsync(response);

        card.GetProperty("positionId").ValueKind.Should().Be(JsonValueKind.String);
        card.GetProperty("positionId").GetGuid().Should().Be(employee.PositionId.Value);
        card.GetProperty("departmentId").ValueKind.Should().Be(JsonValueKind.String);
        card.GetProperty("departmentId").GetGuid().Should().Be(employee.DepartmentId.Value);
        card.GetProperty("hiredAt").GetString().Should().Be("2026-01-15");
        card.GetProperty("updatedAt").GetString().Should().EndWith("Z");

        // The exact field list also excludes directory names, nested objects and deletedAt.
        card.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "id", "firstName", "lastName", "positionId", "departmentId", "hiredAt", "dismissedAt", "updatedAt");
    }

    [Fact]
    public async Task GetById_ShouldReturnNullDismissalDate_WhenEmployeeIsNotDismissed()
    {
        // Arrange
        var employee = await SaveEmployeeAsync(CreateEmployee());

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await ReadSuccessResultAsync(response);
        card.GetProperty("id").GetGuid().Should().Be(employee.Id.Value);
        card.GetProperty("dismissedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetById_ShouldReturnDismissalDate_WhenEmployeeIsDismissedButNotDeleted()
    {
        // Arrange
        var employee = CreateEmployee();
        var dismissalDate = new DateOnly(2026, 10, 31);
        employee.Dismiss(dismissalDate).IsSuccess.Should().BeTrue();
        await SaveEmployeeAsync(employee);

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var card = await ReadSuccessResultAsync(response);
        card.GetProperty("id").GetGuid().Should().Be(employee.Id.Value);
        card.GetProperty("dismissedAt").GetString().Should().Be("2026-10-31");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        await SaveEmployeeAsync(CreateEmployee());
        var unknownId = Guid.NewGuid();

        // Act
        using var response = await _client.GetAsync($"/{unknownId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadErrorAsync(response);
        error.Type.Should().Be(ErrorType.NotFound);
        error.Messages.Should().ContainSingle().Which.Code.Should().Be("employee.not.found");
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenEmployeeIsSoftDeleted()
    {
        // Arrange
        await SaveEmployeeAsync(CreateEmployee());
        var employee = CreateEmployee();
        employee.SoftDelete();
        var deletedEmployee = await SaveEmployeeAsync(employee);
        deletedEmployee.DeletedAt.Should().NotBeNull();

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await ReadErrorAsync(response);
        error.Type.Should().Be(ErrorType.NotFound);
        error.Messages.Should().ContainSingle().Which.Code.Should().Be("employee.not.found");
    }

    [Fact]
    public async Task GetById_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Arrange
        var emptyId = Guid.Empty;

        // Act
        using var response = await _client.GetAsync($"/{emptyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await ReadErrorAsync(response);
        error.Type.Should().Be(ErrorType.Validation);
        var message = error.Messages.Should().ContainSingle().Which;
        message.Code.Should().Be("employee.id.empty");
        message.InvalidField.Should().Be("Id");
    }

    [Fact]
    public async Task GetById_ShouldNotMatchRoute_WhenIdIsNotAValidGuid()
    {
        // Arrange
        const string invalidId = "not-a-guid";

        // Act
        using var response = await _client.GetAsync($"/{invalidId}");

        // Assert: the :guid constraint rejects the URL before the handler runs.
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private static Employee CreateEmployee() => Employee.Create(
        EmployeeName.Create("Ada", "Lovelace").Value,
        new PositionId(Guid.NewGuid()),
        new DepartmentId(Guid.NewGuid()),
        new DateOnly(2026, 1, 15)).Value;

    private async Task<Employee> SaveEmployeeAsync(Employee employee)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync();

        // Use the persisted timestamp, which PostgreSQL stores at microsecond precision.
        return await dbContext.Employees.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(stored => stored.Id == employee.Id);
    }

    private static async Task<JsonElement> ReadSuccessResultAsync(HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<JsonElement>();
        envelope.GetProperty("isError").GetBoolean().Should().BeFalse();
        envelope.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);
        envelope.GetProperty("timeGenerated").GetDateTime().Kind.Should().Be(DateTimeKind.Utc);

        var result = envelope.GetProperty("result");
        result.ValueKind.Should().Be(JsonValueKind.Object);
        return result;
    }

    private static async Task<Error> ReadErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("isError").GetBoolean().Should().BeTrue();
        var envelope = json.Deserialize<Envelope>(JsonSerializerOptions.Web);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Result.Should().BeNull();
        envelope.Error.Should().NotBeNull();
        return envelope.Error!;
    }
}
