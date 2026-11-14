using System.Net;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Positions;
using EmployeeService.Contracts.Employees;
using EmployeeService.Contracts.WebApi.Employees;
using EmployeeService.Infrastructure.Postgres;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;
using Shared.Kernel.Errors;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace EmployeeService.IntegrationTests.Employees.Commands;

// Tests share PostgreSQL sequentially; each test owns its HTTP host and WireMock server.
public class HireEmployeeTests : IClassFixture<EmployeeServiceTestWebFactory>, IAsyncLifetime
{
    private readonly EmployeeServiceTestWebFactory _factory;
    private readonly WireMockServer _directoryServer = WireMockServer.Start();
    private readonly WebApplicationFactory<Program> _application;
    private readonly HttpClient _client;

    public HireEmployeeTests(EmployeeServiceTestWebFactory factory)
    {
        _factory = factory;
        _application = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    ["DirectoryServiceOptions:BaseAddress"] = _directoryServer.Url,
                    ["DirectoryServiceOptions:Timeout"] = "00:00:01",
                })));
        _client = _application.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(15);
    }

    [Fact]
    public async Task Hire_ShouldPersistEmployeeAndReturnLocation_WhenRequestIsValid()
    {
        // Arrange
        var request = CreateRequest() with { FirstName = " Ada ", LastName = " Lovelace " };
        StubPosition(request.PositionId);
        StubDepartment(request.DepartmentId);

        // Act
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<EmployeeDto>>();
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeFalse();
        envelope.Error.Should().BeNull();
        envelope.Result.Should().NotBeNull();

        var card = envelope.Result!;
        card.Id.Should().NotBeEmpty();
        card.FirstName.Should().Be("Ada");
        card.LastName.Should().Be("Lovelace");
        card.PositionId.Should().Be(request.PositionId);
        card.DepartmentId.Should().Be(request.DepartmentId);
        card.HiredAt.Should().Be(new DateOnly(2026, 1, 15));
        card.DismissedAt.Should().BeNull();
        card.UpdatedAt.Should().NotBe(default);
        card.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
        response.Headers.Location.Should().NotBeNull();
        new Uri(_client.BaseAddress!, response.Headers.Location!).AbsolutePath.Should().Be($"/{card.Id}");

        await using var scope = _application.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();
        var employees = await dbContext.Employees.IgnoreQueryFilters().AsNoTracking().ToListAsync();
        var employee = employees.Should().ContainSingle().Which;
        employee.Id.Value.Should().Be(card.Id);
        employee.Name.FirstName.Should().Be("Ada");
        employee.Name.LastName.Should().Be("Lovelace");
        employee.PositionId.Value.Should().Be(request.PositionId);
        employee.DepartmentId.Value.Should().Be(request.DepartmentId);
        employee.HiredAt.Should().Be(new DateOnly(2026, 1, 15));
        employee.DismissedAt.Should().BeNull();
        employee.DeletedAt.Should().BeNull();
        // PostgreSQL stores timestamps at microsecond precision.
        employee.UpdatedAt.Should().BeCloseTo(card.UpdatedAt, TimeSpan.FromMicroseconds(1));

        using var getResponse = await _client.GetAsync(response.Headers.Location);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var storedEnvelope = await getResponse.Content.ReadFromJsonAsync<Envelope<EmployeeDto>>();
        storedEnvelope.Should().NotBeNull();
        storedEnvelope!.IsError.Should().BeFalse();
        storedEnvelope.Result.Should().BeEquivalentTo(card, options => options
            .Using<DateTime>(context => context.Subject.Should()
                .BeCloseTo(context.Expectation, TimeSpan.FromMicroseconds(1)))
            .WhenTypeIs<DateTime>());
    }

    [Theory]
    [InlineData(null, "Lovelace", "employee.name.first.empty")]
    [InlineData("", "Lovelace", "employee.name.first.empty")]
    [InlineData("   ", "Lovelace", "employee.name.first.empty")]
    [InlineData("Ada", null, "employee.name.last.empty")]
    [InlineData("Ada", "", "employee.name.last.empty")]
    [InlineData("Ada", "   ", "employee.name.last.empty")]
    public async Task Hire_ShouldRejectRequestWithoutSaving_WhenNameIsInvalid(
        string? firstName, string? lastName, string expectedCode)
    {
        // Arrange: no directory stubs; validation must reject the request before any HTTP lookups.
        var request = CreateRequest() with { FirstName = firstName!, LastName = lastName! };

        // Act
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        await AssertErrorAsync(response, HttpStatusCode.BadRequest, ErrorType.Validation, expectedCode);
        _directoryServer.LogEntries.Should().BeEmpty();
        await AssertNoEmployeesAsync();
    }

    [Theory]
    [InlineData(true, "employee.position.id.invalid", "PositionId")]
    [InlineData(false, "employee.department.id.invalid", "DepartmentId")]
    public async Task Hire_ShouldRejectRequestWithoutSaving_WhenDirectoryIdIsEmpty(
        bool emptyPositionId, string expectedCode, string expectedField)
    {
        // Arrange
        var request = emptyPositionId
            ? CreateRequest() with { PositionId = Guid.Empty }
            : CreateRequest() with { DepartmentId = Guid.Empty };

        // Act
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, ErrorType.Validation, expectedCode);
        error.InvalidField.Should().Be(expectedField);
        _directoryServer.LogEntries.Should().BeEmpty();
        await AssertNoEmployeesAsync();
    }

    [Fact]
    public async Task Hire_ShouldReturnNotFoundWithoutSaving_WhenPositionDoesNotExist()
    {
        // Arrange
        var request = CreateRequest();
        StubDepartment(request.DepartmentId);
        StubDirectoryResponse($"/positions/{request.PositionId}", HttpStatusCode.NotFound,
            Envelope.Failure(Error.NotFound(new ErrorMessage("position.not.found", "Position was not found"))));

        // Act
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        await AssertErrorAsync(response, HttpStatusCode.NotFound, ErrorType.NotFound, "position.not.found");
        await AssertNoEmployeesAsync();
    }

    [Fact]
    public async Task Hire_ShouldReturnNotFoundWithoutSaving_WhenDepartmentDoesNotExist()
    {
        // Arrange
        var request = CreateRequest();
        StubPosition(request.PositionId);
        StubDirectoryResponse($"/departments/{request.DepartmentId}", HttpStatusCode.NotFound,
            Envelope.Failure(Error.NotFound(new ErrorMessage("department.not.found", "Department was not found"))));

        // Act
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        await AssertErrorAsync(response, HttpStatusCode.NotFound, ErrorType.NotFound, "department.not.found");
        await AssertNoEmployeesAsync();
    }

    [Fact]
    public async Task Hire_ShouldReturnInternalErrorWithoutSaving_WhenDirectoryServiceIsUnavailable()
    {
        // Arrange: keep the configured address, but stop the listener before sending any requests.
        var request = CreateRequest();
        _directoryServer.Stop();

        // Act
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        await AssertErrorAsync(response, HttpStatusCode.InternalServerError, ErrorType.Internal, "internal.server.error");
        await AssertNoEmployeesAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Hire_ShouldReturnInternalErrorWithoutSaving_WhenDirectoryLookupTimesOut(bool delayPosition)
    {
        // Arrange: only one lookup exceeds the outgoing client's one-second timeout.
        var request = CreateRequest();
        var delay = TimeSpan.FromSeconds(5);
        StubPosition(request.PositionId, delayPosition ? delay : TimeSpan.Zero);
        StubDepartment(request.DepartmentId, delayPosition ? TimeSpan.Zero : delay);

        // Act: the test client's timeout is longer, so EmployeeService must return the error itself.
        using var response = await _client.PostAsJsonAsync("/", request);

        // Assert
        await AssertErrorAsync(response, HttpStatusCode.InternalServerError, ErrorType.Internal, "internal.server.error");
        var delayedPath = delayPosition ? $"/positions/{request.PositionId}" : $"/departments/{request.DepartmentId}";
        await AssertNoEmployeesAsync();

        // WireMock writes the log after processing the delayed response, even if the caller has timed out.
        Func<Task> assertRequestWasReceived = () =>
        {
            _directoryServer.FindLogEntries(Request.Create().WithPath(delayedPath).UsingGet()).Should().NotBeEmpty();
            return Task.CompletedTask;
        };
        await assertRequestWasReceived.Should().NotThrowAfterAsync(TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(50));
    }

    public Task InitializeAsync() => _factory.ResetDatabaseAsync();

    public async Task DisposeAsync()
    {
        _client.Dispose();
        try
        {
            await _application.DisposeAsync();
        }
        finally
        {
            _directoryServer.Dispose();
        }
    }

    private static HireEmployeeRequest CreateRequest() => new(
        "Ada", "Lovelace", Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 1, 15));

    private static async Task<ErrorMessage> AssertErrorAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus, ErrorType expectedType, string expectedCode)
    {
        response.StatusCode.Should().Be(expectedStatus);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        envelope.Should().NotBeNull();
        envelope!.IsError.Should().BeTrue();
        envelope.Result.Should().BeNull();
        envelope.Error.Should().NotBeNull();
        envelope.Error!.Type.Should().Be(expectedType);
        var error = envelope.Error.Messages.Should().ContainSingle().Which;
        error.Code.Should().Be(expectedCode);
        response.Headers.Location.Should().BeNull();
        return error;
    }

    private async Task AssertNoEmployeesAsync()
    {
        await using var scope = _application.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();
        var count = await dbContext.Employees.IgnoreQueryFilters().CountAsync();
        count.Should().Be(0);
    }

    private void StubPosition(Guid id, TimeSpan delay = default) =>
        StubDirectoryResponse($"/positions/{id}", HttpStatusCode.OK,
            Envelope<PositionDto>.Ok(new PositionDto(id, "Engineer")), delay);

    private void StubDepartment(Guid id, TimeSpan delay = default) =>
        StubDirectoryResponse($"/departments/{id}", HttpStatusCode.OK,
            Envelope<DepartmentDto>.Ok(new DepartmentDto(id, "Engineering", "engineering", "engineering", null)), delay);

    private void StubDirectoryResponse(string path, HttpStatusCode statusCode, object body, TimeSpan delay = default)
    {
        var response = Response.Create().WithStatusCode(statusCode).WithBodyAsJson(body);
        if (delay > TimeSpan.Zero)
        {
            response.WithDelay(delay);
        }

        _directoryServer
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(response);
    }
}
