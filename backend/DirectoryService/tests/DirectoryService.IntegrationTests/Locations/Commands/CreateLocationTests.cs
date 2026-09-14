using System.Net.Http.Json;
using DirectoryService.Contracts.Locations;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Locations.Commands;

public class CreateLocationTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public CreateLocationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task CreateLocation_ShouldSucceed_WhenRequestIsValid()
    {
        var request = CreateValidRequest();

        var response = await _client.PostAsJsonAsync("api/locations", request);

        Assert.Equal(201, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.NotEqual(Guid.Empty, envelope.Result.Id);
        Assert.Equal("Moscow Office", envelope.Result.Name);
        Assert.Equal("Russia", envelope.Result.Address.Country);
        Assert.Equal("Moscow", envelope.Result.Address.Region);
        Assert.Equal("Moscow", envelope.Result.Address.City);
        Assert.Null(envelope.Result.Address.District);
        Assert.Equal("Tverskaya Street", envelope.Result.Address.Street);
        Assert.Equal("1", envelope.Result.Address.HouseNumber);
        Assert.Equal("125009", envelope.Result.Address.PostalCode);
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnConflict_WhenNameAlreadyExists()
    {
        var request = CreateValidRequest();
        var firstResponse = await _client.PostAsJsonAsync("api/locations", request);
        firstResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsJsonAsync("api/locations", request);

        Assert.Equal(409, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.exists", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task CreateLocation_ShouldReturnBadRequest_WhenNameIsEmpty()
    {
        var request = CreateValidRequest() with { Name = "" };

        var response = await _client.PostAsJsonAsync("api/locations", request);

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.name.empty", envelope.Error!.Messages[0].Code);
    }

    private static CreateLocationRequest CreateValidRequest() =>
        new(
            Name: "Moscow Office",
            Country: "Russia",
            Region: "Moscow",
            City: "Moscow",
            District: null,
            Street: "Tverskaya Street",
            HouseNumber: "1",
            PostalCode: "125009");

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
