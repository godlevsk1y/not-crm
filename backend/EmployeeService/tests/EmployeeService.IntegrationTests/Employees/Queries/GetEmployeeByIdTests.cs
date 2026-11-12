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

[Collection("Employee database")]
public class GetEmployeeByIdTests : IAsyncLifetime
{
    private readonly EmployeeServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public GetEmployeeByIdTests(EmployeeServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetEmployeeById_ShouldReturnPersistedCard_WhenEmployeeIsNotDeleted(bool dismissed)
    {
        // Arrange: arbitrary directory UUIDs, without a DirectoryService instance.
        var employee = CreateEmployee();
        if (dismissed)
        {
            employee.Dismiss(new DateOnly(2026, 10, 31)).IsSuccess.Should().BeTrue();
        }

        var persisted = await SaveEmployeeAsync(employee);

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var envelope = json.Deserialize<Envelope<EmployeeDto>>(JsonSerializerOptions.Web);
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Error.Should().BeNull();
        envelope.Result.Should().Be(new EmployeeDto(
            persisted.Id.Value, persisted.Name.FirstName, persisted.Name.LastName,
            persisted.PositionId.Value, persisted.DepartmentId.Value,
            persisted.HiredAt, persisted.DismissedAt, persisted.UpdatedAt));
        envelope.Result!.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
        json.GetProperty("timeGenerated").GetDateTime().Kind.Should().Be(DateTimeKind.Utc);
        json.GetProperty("isError").GetBoolean().Should().BeFalse();
        json.GetProperty("error").ValueKind.Should().Be(JsonValueKind.Null);

        var card = json.GetProperty("result");
        card.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "id", "firstName", "lastName", "positionId", "departmentId", "hiredAt", "dismissedAt", "updatedAt");
        card.GetProperty("id").GetGuid().Should().Be(persisted.Id.Value);
        card.GetProperty("firstName").GetString().Should().Be(persisted.Name.FirstName);
        card.GetProperty("lastName").GetString().Should().Be(persisted.Name.LastName);
        card.GetProperty("positionId").GetGuid().Should().Be(persisted.PositionId.Value);
        card.GetProperty("departmentId").GetGuid().Should().Be(persisted.DepartmentId.Value);
        card.GetProperty("hiredAt").GetString().Should().Be("2026-01-15");
        if (dismissed)
        {
            card.GetProperty("dismissedAt").GetString().Should().Be("2026-10-31");
        }
        else
        {
            card.GetProperty("dismissedAt").ValueKind.Should().Be(JsonValueKind.Null);
        }

        card.GetProperty("updatedAt").GetString().Should().EndWith("Z");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetEmployeeById_ShouldReturnNotFound_WhenEmployeeIsMissingOrDeleted(bool deleted)
    {
        // Arrange
        await SaveEmployeeAsync(CreateEmployee());
        var employee = CreateEmployee();
        if (deleted)
        {
            employee.SoftDelete();
            var persisted = await SaveEmployeeAsync(employee);
            persisted.DeletedAt.Should().NotBeNull();
        }

        // Act
        using var response = await _client.GetAsync($"/{employee.Id.Value}");

        // Assert
        await AssertErrorResponseAsync(response, HttpStatusCode.NotFound, ErrorType.NotFound, "employee.not.found");
    }

    [Fact]
    public async Task GetEmployeeById_ShouldReturnValidationError_WhenIdIsEmpty()
    {
        // Arrange
        var id = Guid.Empty;

        // Act
        using var response = await _client.GetAsync($"/{id}");

        // Assert
        var error = await AssertErrorResponseAsync(
            response, HttpStatusCode.BadRequest, ErrorType.Validation, "employee.id.empty");
        error.Messages[0].InvalidField.Should().Be("Id");
    }

    [Fact]
    public async Task GetEmployeeById_ShouldNotMatchRoute_WhenIdIsMalformed()
    {
        // Arrange
        const string id = "not-a-uuid";

        // Act
        using var response = await _client.GetAsync($"/{id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task OpenApi_ShouldDescribeEmployeeCardAndResponses()
    {
        // Arrange
        const string path = "/openapi/v1.json";

        // Act
        using var response = await _client.GetAsync(path);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var document = await response.Content.ReadFromJsonAsync<JsonElement>();
        var operation = document.GetProperty("paths").GetProperty("/{id}").GetProperty("get");
        operation.GetProperty("operationId").GetString().Should().Be("GetEmployeeById");
        operation.GetProperty("responses").EnumerateObject().Select(property => property.Name)
            .Should().Contain(["200", "400", "404"]);
        var schema = document.GetProperty("components").GetProperty("schemas").GetProperty("EmployeeDto");
        var properties = schema.GetProperty("properties");
        properties.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "id", "firstName", "lastName", "positionId", "departmentId", "hiredAt", "dismissedAt", "updatedAt");
        properties.GetProperty("positionId").GetProperty("format").GetString().Should().Be("uuid");
        properties.GetProperty("departmentId").GetProperty("format").GetString().Should().Be("uuid");
        properties.GetProperty("hiredAt").GetProperty("format").GetString().Should().Be("date");
        properties.GetProperty("dismissedAt").GetProperty("format").GetString().Should().Be("date");
        properties.GetProperty("updatedAt").GetProperty("format").GetString().Should().Be("date-time");
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

        // Read the stored timestamp back at PostgreSQL's microsecond precision.
        return await dbContext.Employees.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(stored => stored.Id == employee.Id);
    }

    private static async Task<Error> AssertErrorResponseAsync(
        HttpResponseMessage response, HttpStatusCode status, ErrorType type, string code)
    {
        response.StatusCode.Should().Be(status);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Result.Should().BeNull();
        envelope.Error.Should().NotBeNull();
        envelope.Error!.Type.Should().Be(type);
        var message = envelope.Error.Messages.Should().ContainSingle().Which;
        message.Code.Should().Be(code);
        message.Message.Should().NotBeNullOrWhiteSpace();
        return envelope.Error;
    }
}
