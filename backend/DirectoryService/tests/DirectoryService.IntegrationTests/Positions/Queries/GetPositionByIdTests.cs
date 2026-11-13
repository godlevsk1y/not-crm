using System.Net.Http.Json;
using System.Text.Json;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;

namespace DirectoryService.IntegrationTests.Positions.Queries;

public class GetPositionByIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public GetPositionByIdTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnPosition_WithoutChangingStoredData()
    {
        var position = await CreatePositionAsync();

        using var response = await _client.GetAsync($"positions/{position.Id.Value}");

        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.Equal(new PositionDto(position.Id.Value, position.Name.Value), envelope.Result);
        await AssertPositionUnchangedAsync(position);
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnNotFound_WhenPositionDoesNotExist()
    {
        using var response = await _client.GetAsync($"positions/{Guid.NewGuid()}");

        await AssertNotFoundAsync(response);
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnNotFound_WithoutChangingArchivedPosition()
    {
        var position = await CreatePositionAsync(isArchived: true);

        using var response = await _client.GetAsync($"positions/{position.Id.Value}");

        await AssertNotFoundAsync(response);
        await AssertPositionUnchangedAsync(position);
    }

    [Fact]
    public async Task GetPositionById_ShouldReturnCreatedPosition_FromLocationHeader()
    {
        using var createResponse = await _client.PostAsJsonAsync(
            "positions", new CreatePositionRequest("Software Engineer"));
        Assert.Equal(201, (int)createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Envelope<PositionDto>>();
        Assert.NotNull(created?.Result);
        Assert.NotNull(createResponse.Headers.Location);
        Assert.Equal($"/positions/{created.Result.Id}", createResponse.Headers.Location.OriginalString);

        using var response = await _client.GetAsync(createResponse.Headers.Location);

        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.Equal(created.Result, envelope.Result);
    }

    [Fact]
    public async Task GetPositionById_ShouldDocumentContractAndErrors_InOpenApi()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("openapi/v1.json"));
        var paths = document.RootElement.GetProperty("paths");
        var operation = paths.GetProperty("/positions/{id}").GetProperty("get");
        var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("id", parameter.GetProperty("name").GetString());
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());
        Assert.Equal("uuid", parameter.GetProperty("schema").GetProperty("format").GetString());

        var responses = operation.GetProperty("responses");
        var example = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("example").GetProperty("result");
        Assert.True(example.TryGetProperty("id", out _));
        Assert.True(example.TryGetProperty("name", out _));
        var notFound = responses.GetProperty("404");
        Assert.Contains("position.not.found", notFound.GetProperty("description").GetString(), StringComparison.Ordinal);
        var error = notFound.GetProperty("content").GetProperty("application/json")
            .GetProperty("examples").GetProperty("position.not.found").GetProperty("value");
        Assert.True(error.GetProperty("isError").GetBoolean());
        Assert.Equal("position.not.found", error.GetProperty("error")
            .GetProperty("messages")[0].GetProperty("code").GetString());

        var createDescription = paths.GetProperty("/positions").GetProperty("post").GetProperty("description").GetString();
        Assert.Contains("GET this URI", createDescription, StringComparison.Ordinal);

        using var health = await _client.GetAsync("health/live");
        Assert.Equal(200, (int)health.StatusCode);
    }

    private static async Task AssertNotFoundAsync(HttpResponseMessage response)
    {
        Assert.Equal(404, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Error);
        Assert.Equal("position.not.found", Assert.Single(envelope.Error.Messages).Code);
    }

    private async Task<Position> CreatePositionAsync(bool isArchived = false)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var position = Position.Create(PositionName.Create("Software Engineer").Value);
        if (isArchived)
            position.SoftDelete();
        context.Positions.Add(position);
        await context.SaveChangesAsync();
        await context.Entry(position).ReloadAsync();
        return position;
    }

    private async Task AssertPositionUnchangedAsync(Position expected)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var actual = await context.Positions.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(p => p.Id == expected.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.CreatedAt, actual.CreatedAt);
        Assert.Equal(expected.UpdatedAt, actual.UpdatedAt);
        Assert.Equal(expected.DeletedAt, actual.DeletedAt);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
