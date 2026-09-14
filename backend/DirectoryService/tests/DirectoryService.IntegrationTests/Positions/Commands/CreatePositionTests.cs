using System.Net.Http.Json;
using DirectoryService.Contracts.Positions;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Positions.Commands;

public class CreatePositionTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public CreatePositionTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task CreatePosition_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new CreatePositionRequest("Software Engineer");

        var response = await _client.PostAsJsonAsync("api/positions", request);

        Assert.Equal(201, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.NotEqual(Guid.Empty, envelope.Result.Id);
        Assert.Equal("Software Engineer", envelope.Result.Name);
    }

    [Fact]
    public async Task CreatePosition_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var request = new CreatePositionRequest("");

        var response = await _client.PostAsJsonAsync("api/positions", request);

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("position.name.empty", envelope.Error!.Messages[0].Code);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
