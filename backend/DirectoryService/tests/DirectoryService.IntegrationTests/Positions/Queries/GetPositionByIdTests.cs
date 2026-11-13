using System.Net.Http.Json;
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
