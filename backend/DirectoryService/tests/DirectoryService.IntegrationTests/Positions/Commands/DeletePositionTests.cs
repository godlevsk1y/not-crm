using System.Net.Http.Json;
using DirectoryService.Contracts.Positions;
using DirectoryService.Domain.Ids;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Web.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests.Positions.Commands;

public class DeletePositionTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly Func<Task> _resetDatabase;

    public DeletePositionTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task DeletePosition_ShouldSucceed_WhenPositionExists()
    {
        var position = await CreatePositionAsync();

        var response = await _client.DeleteAsync($"api/positions/{position.Id}");

        Assert.Equal(204, (int)response.StatusCode);

        var secondResponse = await _client.DeleteAsync($"api/positions/{position.Id}");
        Assert.Equal(404, (int)secondResponse.StatusCode);

        var envelope = await secondResponse.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("position.not.found", envelope.Error!.Messages[0].Code);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();

        var deletedPosition = await dbContext.Positions
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(p => p.Id == new PositionId(position.Id));

        Assert.NotNull(deletedPosition);
        Assert.NotNull(deletedPosition.DeletedAt);
    }

    [Fact]
    public async Task DeletePosition_ShouldReturnNotFound_WhenPositionDoesNotExist()
    {
        var positionId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"api/positions/{positionId}");

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("position.not.found", envelope.Error!.Messages[0].Code);
    }

    private async Task<PositionDto> CreatePositionAsync()
    {
        var request = new CreatePositionRequest("Software Engineer");

        var response = await _client.PostAsJsonAsync("api/positions", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionDto>>();
        Assert.NotNull(envelope?.Result);

        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
