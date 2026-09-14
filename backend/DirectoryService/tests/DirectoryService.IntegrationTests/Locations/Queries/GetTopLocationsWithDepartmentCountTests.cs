using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Locations.Queries;

public class GetTopLocationsWithDepartmentCountTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetTopLocationsWithDepartmentCountTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetTopLocationsWithDepartmentCount_ShouldReturnFiveLocationsOrderedByDepartmentCount()
    {
        var moscowId = await CreateLocationAsync("Moscow Office", "Moscow");
        var berlinId = await CreateLocationAsync("Berlin Office", "Berlin");
        var zurichId = await CreateLocationAsync("Zurich Office", "Zurich");
        var amsterdamId = await CreateLocationAsync("Amsterdam Office", "Amsterdam");
        var parisId = await CreateLocationAsync("Paris Office", "Paris");
        await CreateLocationAsync("Oslo Office", "Oslo");

        await CreateDepartmentAsync("Finance Team", "finance-team", moscowId);
        await CreateDepartmentAsync("Product Team", "product-team", moscowId);
        await CreateDepartmentAsync("Sales Team", "sales-team", moscowId);
        await CreateDepartmentAsync("Human Resources", "human-resources", berlinId);
        await CreateDepartmentAsync("Legal Team", "legal-team", berlinId);
        await CreateDepartmentAsync("Quality Assurance", "quality-assurance", zurichId);
        await CreateDepartmentAsync("Research Team", "research-team", zurichId);
        await CreateDepartmentAsync("Marketing Team", "marketing-team", amsterdamId);
        await CreateDepartmentAsync("Support Team", "support-team", parisId);

        var response = await _client.GetAsync("api/locations/top");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<List<LocationWithDepartmentCountDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Collection(
            envelope.Result,
            item => AssertLocation(item, moscowId, "Moscow Office", 3),
            item => AssertLocation(item, berlinId, "Berlin Office", 2),
            item => AssertLocation(item, zurichId, "Zurich Office", 2),
            item => AssertLocation(item, amsterdamId, "Amsterdam Office", 1),
            item => AssertLocation(item, parisId, "Paris Office", 1));
    }

    [Fact]
    public async Task GetTopLocationsWithDepartmentCount_ShouldReturnEmptyList_WhenLocationsDoNotExist()
    {
        var response = await _client.GetAsync("api/locations/top");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<List<LocationWithDepartmentCountDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Empty(envelope.Result);
    }

    private static void AssertLocation(
        LocationWithDepartmentCountDto item,
        Guid expectedId,
        string expectedName,
        long expectedDepartmentCount)
    {
        Assert.Equal(expectedId, item.Location.Id);
        Assert.Equal(expectedName, item.Location.Name);
        Assert.Equal(expectedDepartmentCount, item.DepartmentCount);
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
