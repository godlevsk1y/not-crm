using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Locations.Queries;

public class GetLocationListTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetLocationListTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetLocationList_ShouldReturnSelectedLocations_WhenRequestIsValid()
    {
        var berlinId = await CreateLocationAsync("Berlin Office", "Berlin");
        var moscowId = await CreateLocationAsync("Moscow Office", "Moscow");
        await CreateLocationAsync("Remote Hub", "Lisbon");

        await CreateDepartmentAsync("Finance Team", "finance-team", berlinId);
        await CreateDepartmentAsync("Product Team", "product-team", berlinId);
        await CreateDepartmentAsync("Support Team", "support-team", moscowId);

        var response = await _client.GetAsync(
            "api/locations?search=office&minDepartmentCount=1" +
            "&sortBy=departmentCount&sortDirection=desc&page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<LocationListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);

        var locations = envelope.Result.Results.ToList();
        Assert.Collection(
            locations,
            location =>
            {
                Assert.Equal(berlinId, location.Id);
                Assert.Equal("Berlin Office", location.Name);
                Assert.Equal(2, location.DepartmentCount);
            },
            location =>
            {
                Assert.Equal(moscowId, location.Id);
                Assert.Equal("Moscow Office", location.Name);
                Assert.Equal(1, location.DepartmentCount);
            });
    }

    [Fact]
    public async Task GetLocationList_ShouldReturnEmptyList_WhenLocationsNotFound()
    {
        await CreateLocationAsync("Moscow Office", "Moscow");

        var response = await _client.GetAsync(
            "api/locations?search=missing&sortBy=name&sortDirection=asc&page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<LocationListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(0, envelope.Result.TotalCount);
        Assert.Empty(envelope.Result.Results);
    }

    [Fact]
    public async Task GetLocationList_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        var response = await _client.GetAsync(
            "api/locations?sortBy=name&sortDirection=invalid&page=1&pageSize=5");

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("locations.sort.direction.invalid", envelope.Error!.Messages[0].Code);
    }

    private async Task<Guid> CreateLocationAsync(string name, string city)
    {
        var request = new CreateLocationRequest(
            Name: name,
            Country: "Russia",
            Region: null,
            City: city,
            District: null,
            Street: "Central Street",
            HouseNumber: "1",
            PostalCode: null);

        var response = await _client.PostAsJsonAsync("api/locations", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDto>>();
        Assert.NotNull(envelope?.Result);

        return envelope.Result.Id;
    }

    private async Task CreateDepartmentAsync(string name, string slug, Guid locationId)
    {
        var request = new CreateDepartmentRequest(
            Name: name,
            Slug: slug,
            LocationIds: [locationId],
            ParentId: null);

        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
