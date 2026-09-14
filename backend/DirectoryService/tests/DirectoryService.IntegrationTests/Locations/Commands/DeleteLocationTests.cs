using System.Net.Http.Json;
using DirectoryService.Contracts.Locations;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Locations.Commands;

public class DeleteLocationTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public DeleteLocationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task DeleteLocation_ShouldSucceed_WhenLocationExists()
    {
        var location = await CreateLocationAsync();

        var response = await _client.DeleteAsync($"api/locations/{location.Id}");

        Assert.Equal(204, (int)response.StatusCode);

        var getResponse = await _client.GetAsync($"api/locations/{location.Id}");
        Assert.Equal(404, (int)getResponse.StatusCode);

        var envelope = await getResponse.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task DeleteLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        var locationId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"api/locations/{locationId}");

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.not.found", envelope.Error!.Messages[0].Code);
    }

    private async Task<LocationDto> CreateLocationAsync()
    {
        var request = new CreateLocationRequest(
            Name: "Moscow Office",
            Country: "Russia",
            Region: "Moscow",
            City: "Moscow",
            District: null,
            Street: "Tverskaya Street",
            HouseNumber: "1",
            PostalCode: "125009");

        var response = await _client.PostAsJsonAsync("api/locations", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDto>>();
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Result);

        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
