using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.WebApi.Departments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;
using Serilog.Formatting.Display;
using Shared.Framework.Results;

namespace DirectoryService.IntegrationTests.Logging;

public class RequestLoggingTests : IClassFixture<LoggingTestWebFactory>, IAsyncLifetime
{
    private readonly LoggingTestWebFactory _factory;
    private readonly HttpClient _client;

    public RequestLoggingTests(LoggingTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Success_ShouldCorrelateApplicationAndSingleHttpEvent()
    {
        await CreateDepartmentAsync("Engineering", "engineering");

        var http = AssertHttpEvent("/departments", 201, LogEventLevel.Information);
        var application = Assert.Single(_factory.Logs.Events, e => string.Equals(e.MessageTemplate.Text,
            "Department {DepartmentId} created", StringComparison.Ordinal));
        Assert.NotNull(http.TraceId);
        Assert.Equal(http.TraceId, application.TraceId);
        Assert.Equal(http.Properties["TraceId"], application.Properties["TraceId"]);
        Assert.Equal("CreateDepartmentHandler", Scalar(application, "SourceName"));
        AssertNoFailures();
        Assert.DoesNotContain(_factory.Logs.Events, e =>
            Scalar(e, "SourceContext") is string source &&
            (source.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) ||
             source.StartsWith("Npgsql", StringComparison.Ordinal) ||
             source.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Request_ShouldNotLogQueryTokensHeadersOrBody()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "departments?token=query-secret-136")
        {
            Content = JsonContent.Create(new
            {
                Name = "Engineering",
                Slug = "engineering",
                LocationIds = Array.Empty<Guid>(),
                Password = "body-secret-136",
                ConnectionString = "connection-secret-136",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "header-secret-136");

        var response = await _client.SendAsync(request);

        Assert.Equal(201, (int)response.StatusCode);
        AssertHttpEvent("/departments", 201, LogEventLevel.Information);
        Assert.All(_factory.Logs.Events, e =>
        {
            var serialized = string.Join(" ", e.Properties.Select(p => $"{p.Key}={p.Value}"));
            Assert.DoesNotContain("secret-136", serialized, StringComparison.Ordinal);
            Assert.DoesNotContain("secret-136", e.RenderMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Validation_ShouldLogOneInformationSummaryWithoutStack()
    {
        var response = await _client.PostAsJsonAsync("departments", new CreateDepartmentRequest("", "", [], null));

        Assert.Equal(400, (int)response.StatusCode);
        AssertHttpEvent("/departments", 400, LogEventLevel.Information);
        AssertNoFailures();
    }

    [Fact]
    public async Task NotFound_ShouldLogOneInformationSummaryWithoutStack()
    {
        var path = $"/departments/{Guid.NewGuid()}";
        var response = await _client.GetAsync(path);

        Assert.Equal(404, (int)response.StatusCode);
        AssertHttpEvent(path, 404, LogEventLevel.Information);
        AssertNoFailures();
    }

    [Fact]
    public async Task DatabaseConflict_ShouldLogOneInformationSummaryWithoutStack()
    {
        var response = await _client.PostAsync("/__logging/database-conflict", content: null);

        Assert.Equal(409, (int)response.StatusCode);
        AssertHttpEvent("/__logging/database-conflict", 409, LogEventLevel.Information);
        AssertNoFailures();
    }

    [Fact]
    public async Task UnexpectedDatabaseFailure_ShouldPreserveOneStack()
    {
        var request = new DirectoryService.Contracts.Locations.CreateLocationRequest(
            "Office", new string('x', 61), "Moscow", "Moscow", null, "Tverskaya", "1", "125009");
        var response = await _client.PostAsJsonAsync("locations", request);

        Assert.Equal(500, (int)response.StatusCode);
        AssertHttpEvent("/locations", 500, LogEventLevel.Error);
        var failure = Assert.Single(_factory.Logs.Events, e => e.Exception is not null);
        Assert.NotNull(failure.Exception!.InnerException);
        Assert.NotEmpty(failure.Exception.StackTrace!);
    }

    [Theory]
    [InlineData("exception", "Unexpected request failure")]
    [InlineData("transaction-exception", "Unexpected transaction failure")]
    [InlineData("unrelated-cancellation", "Internal timeout")]
    public async Task UnexpectedFailure_ShouldPreserveOneStackAndLog500(string endpoint, string message)
    {
        var path = $"/__logging/{endpoint}";
        var response = await _client.GetAsync(path);

        Assert.Equal(500, (int)response.StatusCode);
        var http = AssertHttpEvent(path, 500, LogEventLevel.Error);
        Assert.Null(http.Exception);
        var failure = Assert.Single(_factory.Logs.Events, e => e.Exception is not null);
        Assert.Equal(message, failure.Exception!.Message);
        Assert.NotEmpty(failure.Exception.StackTrace!);
        Assert.Equal(http.TraceId, failure.TraceId);
        Assert.DoesNotContain(message, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("transaction-cancelled")]
    public async Task ClientCancellation_ShouldLog499WithoutErrorOrStack(string endpoint)
    {
        var path = $"/__logging/{endpoint}";
        var response = await _client.GetAsync(path);

        Assert.Equal(499, (int)response.StatusCode);
        var http = AssertHttpEvent(path, 499, LogEventLevel.Information);
        Assert.True(Assert.IsType<bool>(Scalar(http, "RequestAborted")));
        AssertNoFailures();
    }

    [Fact]
    public async Task StartedResponseFailure_ShouldPreserveOneStackWithoutRewritingResponse()
    {
        await Assert.ThrowsAsync<HttpRequestException>(() => _client.GetAsync("/__logging/started-response"));

        var failure = Assert.Single(_factory.Logs.Events, e => e.Exception is not null);
        Assert.Equal("Failure after response started", failure.Exception!.Message);
        Assert.Equal("/__logging/started-response", Scalar(failure, "RequestPath"));
        var http = AssertHttpEvent("/__logging/started-response", 200, LogEventLevel.Error);
        Assert.True(Assert.IsType<bool>(Scalar(http, "RequestFailed")));
        Assert.True(Assert.IsType<bool>(Scalar(http, "ResponseStarted")));
    }

    [Fact]
    public async Task StartedResponseCancellation_ShouldPreserveResponseWithoutError()
    {
        var response = await _client.GetAsync("/__logging/started-cancelled");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.Equal("Partial response", await response.Content.ReadAsStringAsync());
        AssertHttpEvent("/__logging/started-cancelled", 200, LogEventLevel.Information);
        AssertNoFailures();
    }

    [Fact]
    public async Task HealthyProbe_ShouldBeQuietAndUnhealthyProbeShouldRemainVisible()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(200, (int)response.StatusCode);
        Assert.DoesNotContain(_factory.Logs.Events, e => Scalar(e, "RequestPath") is "/health/live");

        _factory.Unhealthy = true;
        response = await _client.GetAsync("/health/live");

        Assert.Equal(503, (int)response.StatusCode);
        AssertHttpEvent("/health/live", 503, LogEventLevel.Error);
    }

    [Fact]
    public async Task Transfer_ShouldLogIdentifiersOnlyAfterSuccessfulChange()
    {
        var parent = await CreateDepartmentAsync("Engineering", "engineering");
        var child = await CreateDepartmentAsync("Backend", "backend");
        _factory.Logs.Clear();

        var response = await _client.PutAsJsonAsync(
            $"departments/{child.Id}/parent", new TransferDepartmentRequest(parent.Id));

        Assert.Equal(200, (int)response.StatusCode);
        var transfer = Assert.Single(_factory.Logs.Events, e => e.Properties.ContainsKey("OldParentId"));
        Assert.Equal(child.Id, Scalar(transfer, "DepartmentId"));
        Assert.Null(Scalar(transfer, "OldParentId"));
        Assert.Equal(parent.Id, Scalar(transfer, "NewParentId"));

        _factory.Logs.Clear();
        response = await _client.PutAsJsonAsync(
            $"departments/{child.Id}/parent", new TransferDepartmentRequest(parent.Id));
        Assert.Equal(200, (int)response.StatusCode);
        response = await _client.PutAsJsonAsync(
            $"departments/{parent.Id}/parent", new TransferDepartmentRequest(child.Id));
        Assert.Equal(409, (int)response.StatusCode);
        Assert.DoesNotContain(_factory.Logs.Events, e => e.Properties.ContainsKey("OldParentId"));
        AssertNoFailures();
    }

    [Fact]
    public async Task Console_ShouldUseShortSourceAndKeepDiagnosticPropertiesInEvent()
    {
        await CreateDepartmentAsync("Engineering", "engineering");
        var application = Assert.Single(_factory.Logs.Events, e => e.Properties.ContainsKey("DepartmentId"));
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var template = configuration["Serilog:WriteTo:0:Args:outputTemplate"]!;
        var formatter = new MessageTemplateTextFormatter(template, CultureInfo.InvariantCulture);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        formatter.Format(application, output);

        Assert.Contains("CreateDepartmentHandler", output.ToString(), StringComparison.Ordinal);
        Assert.Contains($"TraceId={application.TraceId}", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("DirectoryService.Core.Features", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("ServiceName", output.ToString(), StringComparison.Ordinal);
        Assert.Equal("DirectoryService", Scalar(application, "ServiceName"));
        Assert.NotNull(Scalar(application, "SourceContext"));
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(string name, string slug)
    {
        var response = await _client.PostAsJsonAsync("departments", new CreateDepartmentRequest(name, slug, [], null));
        Assert.Equal(201, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        return envelope.Result;
    }

    private LogEvent AssertHttpEvent(string path, int statusCode, LogEventLevel level)
    {
        var http = Assert.Single(_factory.Logs.Events, e =>
            e.Properties.ContainsKey("Elapsed") && Equals(Scalar(e, "RequestPath"), path));
        Assert.Equal(statusCode, Scalar(http, "StatusCode"));
        Assert.Equal(level, http.Level);
        Assert.NotNull(Scalar(http, "RequestMethod"));
        return http;
    }

    private void AssertNoFailures() => Assert.All(_factory.Logs.Events, e =>
    {
        Assert.Null(e.Exception);
        Assert.True(e.Level < LogEventLevel.Warning, e.RenderMessage(CultureInfo.InvariantCulture));
    });

    private static object? Scalar(LogEvent logEvent, string name) =>
        logEvent.Properties.TryGetValue(name, out var value) ? ((ScalarValue)value).Value : null;

    public Task InitializeAsync()
    {
        _factory.Unhealthy = false;
        _factory.Logs.Clear();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
