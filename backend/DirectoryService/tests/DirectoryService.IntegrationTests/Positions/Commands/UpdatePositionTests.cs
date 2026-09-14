using System.Net.Http.Json;
using DirectoryService.Contracts.Positions;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Positions.Commands;

public class UpdatePositionTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public UpdatePositionTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task UpdatePosition_ShouldSucceed_WhenRequestIsValid()
    {
        var position = await CreatePositionAsync();
        var request = new UpdatePositionRequest("Senior Software Engineer");

        var response = await _client.PatchAsJsonAsync($"api/positions/{position.Id}", request);

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<Guid>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.Equal(position.Id, envelope.Result);
    }

    [Fact]
    public async Task UpdatePosition_ShouldReturnNotFound_WhenPositionDoesNotExist()
    {
        var positionId = Guid.NewGuid();
        var request = new UpdatePositionRequest("Senior Software Engineer");

        var response = await _client.PatchAsJsonAsync($"api/positions/{positionId}", request);

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("position.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task UpdatePosition_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var position = await CreatePositionAsync();
        var request = new UpdatePositionRequest("");

        var response = await _client.PatchAsJsonAsync($"api/positions/{position.Id}", request);

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("position.name.empty", envelope.Error!.Messages[0].Code);
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
