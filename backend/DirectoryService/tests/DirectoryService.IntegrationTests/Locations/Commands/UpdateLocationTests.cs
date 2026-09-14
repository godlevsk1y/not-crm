using System.Net.Http.Json;
using DirectoryService.Contracts.Locations;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Locations.Commands;

public class UpdateLocationTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public UpdateLocationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task UpdateLocation_ShouldSucceed_WhenRequestIsValid()
    {
        var location = await CreateLocationAsync();
        var request = new UpdateLocationRequest(
            Name: "Saint Petersburg Office",
            Country: "Russia",
            Region: "Saint Petersburg",
            City: "Saint Petersburg",
            District: "Tsentralny District",
            Street: "Nevsky Prospekt",
            HouseNumber: "28",
            PostalCode: "191186");

        var response = await _client.PatchAsJsonAsync($"api/locations/{location.Id}", request);

        Assert.Equal(200, (int)response.StatusCode);

        var updateEnvelope = await response.Content.ReadFromJsonAsync<Envelope<Guid>>();
        Assert.NotNull(updateEnvelope);
        Assert.False(updateEnvelope.IsError);
        Assert.Equal(location.Id, updateEnvelope.Result);

        var getResponse = await _client.GetAsync($"api/locations/{location.Id}");
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<Envelope<LocationDto>>();

        Assert.NotNull(getEnvelope);
        Assert.False(getEnvelope.IsError);
        Assert.NotNull(getEnvelope.Result);
        Assert.Equal("Saint Petersburg Office", getEnvelope.Result.Name);
        Assert.Equal("Russia", getEnvelope.Result.Address.Country);
        Assert.Equal("Saint Petersburg", getEnvelope.Result.Address.Region);
        Assert.Equal("Saint Petersburg", getEnvelope.Result.Address.City);
        Assert.Equal("Tsentralny District", getEnvelope.Result.Address.District);
        Assert.Equal("Nevsky Prospekt", getEnvelope.Result.Address.Street);
        Assert.Equal("28", getEnvelope.Result.Address.HouseNumber);
        Assert.Equal("191186", getEnvelope.Result.Address.PostalCode);
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        var locationId = Guid.NewGuid();
        var request = new UpdateLocationRequest(
            Name: "Saint Petersburg Office",
            Country: null,
            Region: null,
            City: null,
            District: null,
            Street: null,
            HouseNumber: null,
            PostalCode: null);

        var response = await _client.PatchAsJsonAsync($"api/locations/{locationId}", request);

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task UpdateLocation_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var location = await CreateLocationAsync();
        var request = new UpdateLocationRequest(
            Name: "",
            Country: null,
            Region: null,
            City: null,
            District: null,
            Street: null,
            HouseNumber: null,
            PostalCode: null);

        var response = await _client.PatchAsJsonAsync($"api/locations/{location.Id}", request);

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.name.empty", envelope.Error!.Messages[0].Code);
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
