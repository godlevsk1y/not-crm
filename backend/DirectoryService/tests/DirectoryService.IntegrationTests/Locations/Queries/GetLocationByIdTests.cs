using System.Net.Http.Json;
using DirectoryService.Contracts.Locations;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Locations.Queries;

public class GetLocationByIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetLocationByIdTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetLocationById_ShouldReturnLocation_WhenLocationExists()
    {
        var createdLocation = await CreateLocationAsync();

        var response = await _client.GetAsync($"api/locations/{createdLocation.Id}");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(createdLocation, envelope.Result);
    }

    [Fact]
    public async Task GetLocationById_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        var response = await _client.GetAsync($"api/locations/{Guid.NewGuid()}");

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
        Assert.NotNull(envelope?.Result);

        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
