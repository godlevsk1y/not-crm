using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using Shared.Framework.Results;
using Shared.Kernel.Results;

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

    [Theory]
    [InlineData(2)]
    [InlineData(20)]
    public async Task GetLocationList_ShouldPreserveTotalCount_WhenPageIsOutOfRange(int page)
    {
        var berlin = await CreateLocationAsync("Berlin Office", "Berlin");
        var moscow = await CreateLocationAsync("Moscow Office", "Moscow");
        var remote = await CreateLocationAsync("Remote Hub", "Lisbon");
        await CreateLocationAsync("Empty Office", "Paris");
        await CreateDepartmentAsync("Finance", "finance", berlin);
        await CreateDepartmentAsync("Product", "product", moscow);
        await CreateDepartmentAsync("Remote", "remote", remote);
        var deleted = await CreateLocationAsync("Deleted Office", "Rome");
        await CreateDepartmentAsync("Deleted", "deleted", deleted);
        var deleteResponse = await _client.DeleteAsync($"api/locations/{deleted}");
        Assert.Equal(204, (int)deleteResponse.StatusCode);

        var response = await _client.GetAsync(
            $"api/locations?search=office&minDepartmentCount=1&sortBy=name&sortDirection=asc&page={page}&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<LocationListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2, envelope.Result.TotalCount);
        Assert.Equal(page, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);
        Assert.Empty(envelope.Result.Results);
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

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetLocationList_ShouldReturnEmptyList_WhenLocationsNotFound(int page)
    {
        await CreateLocationAsync("Moscow Office", "Moscow");

        var response = await _client.GetAsync(
            $"api/locations?search=missing&sortBy=name&sortDirection=asc&page={page}&pageSize=5");

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

    [Fact]
    public async Task GetLocationList_ShouldFilterByDepartment_WhenOneDepartmentIsSelected()
    {
        var berlin = await CreateLocationAsync("Berlin Office", "Berlin");
        var moscow = await CreateLocationAsync("Moscow Office", "Moscow");
        var deleted = await CreateLocationAsync("Deleted Office", "Rome");
        await CreateLocationAsync("Unlinked Office", "Paris");
        var department = await CreateDepartmentAsync("Finance", "finance", berlin, deleted);
        await CreateDepartmentAsync("Product", "product", moscow);
        var deleteResponse = await _client.DeleteAsync($"api/locations/{deleted}");
        deleteResponse.EnsureSuccessStatusCode();

        var result = await GetLocationsAsync($"departmentIds={department}");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(berlin, Assert.Single(result.Results).Id);
    }

    [Fact]
    public async Task GetLocationList_ShouldReturnUnionWithoutDuplicates_WhenMultipleDepartmentsAreSelected()
    {
        var berlin = await CreateLocationAsync("Berlin Office", "Berlin");
        var moscow = await CreateLocationAsync("Moscow Office", "Moscow");
        var remote = await CreateLocationAsync("Remote Hub", "Lisbon");
        var unrelated = await CreateLocationAsync("Other Office", "Paris");
        await CreateLocationAsync("Unlinked Office", "Rome");
        var finance = await CreateDepartmentAsync("Finance", "finance", berlin, moscow);
        var product = await CreateDepartmentAsync("Product", "product", berlin, remote);
        await CreateDepartmentAsync("Other", "other", unrelated);

        var result = await GetLocationsAsync($"departmentIds={finance},{product},{finance}");

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(new[] { berlin, moscow, remote }, result.Results.Select(location => location.Id));
        Assert.Equal(2, result.Results.First().DepartmentCount);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public async Task GetLocationList_ShouldReturnEmptyList_WhenSelectedDepartmentHasNoLocations(
        int page, bool departmentExists)
    {
        var location = await CreateLocationAsync("Moscow Office", "Moscow");
        await CreateDepartmentAsync("Product", "product", location);
        var department = departmentExists
            ? await CreateDepartmentAsync("Empty", "empty")
            : Guid.NewGuid();

        var result = await GetLocationsAsync($"departmentIds={department}&page={page}");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("departmentIds=")]
    [InlineData("departmentIds=%20%20")]
    public async Task GetLocationList_ShouldPreserveExistingBehavior_WhenDepartmentFilterIsAbsentOrEmpty(
        string filter)
    {
        var berlin = await CreateLocationAsync("Berlin Office", "Berlin");
        var moscow = await CreateLocationAsync("Moscow Office", "Moscow");
        await CreateDepartmentAsync("Finance", "finance", berlin);

        var result = await GetLocationsAsync(filter);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(new[] { berlin, moscow }, result.Results.Select(location => location.Id));
    }

    [Fact]
    public async Task GetLocationList_ShouldCombineFiltersAndSortByAllDepartmentLinks()
    {
        var berlin = await CreateLocationAsync("Berlin Office", "Berlin");
        var moscow = await CreateLocationAsync("Moscow Office", "Moscow");
        var singleLink = await CreateLocationAsync("Single Office", "Paris");
        var remote = await CreateLocationAsync("Remote Hub", "Lisbon");
        var unrelated = await CreateLocationAsync("Other Office", "Rome");
        var selected = await CreateDepartmentAsync("Selected", "selected", berlin, moscow, singleLink, remote);
        await CreateDepartmentAsync("Finance", "finance", berlin, moscow, remote, unrelated);
        await CreateDepartmentAsync("Product", "product", berlin, unrelated);

        var result = await GetLocationsAsync(
            $"departmentIds={selected}&search=office&minDepartmentCount=2&sortBy=departmentCount&sortDirection=desc");

        Assert.Equal(2, result.TotalCount);
        Assert.Collection(result.Results,
            location =>
            {
                Assert.Equal(berlin, location.Id);
                Assert.Equal(3, location.DepartmentCount);
            },
            location =>
            {
                Assert.Equal(moscow, location.Id);
                Assert.Equal(2, location.DepartmentCount);
            });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetLocationList_ShouldPaginateUniqueMatches_WhenDepartmentsAreSelected(int page)
    {
        await CreateLocationAsync("A Unlinked Office", "Paris");
        var unrelated = await CreateLocationAsync("B Other Office", "Rome");
        await CreateDepartmentAsync("Other", "other", unrelated);
        var locations = new List<Guid>();
        for (var i = 1; i <= 7; i++)
        {
            locations.Add(await CreateLocationAsync($"Office {i}", "Moscow"));
        }

        var finance = await CreateDepartmentAsync("Finance", "finance", locations.ToArray());
        var product = await CreateDepartmentAsync("Product", "product", locations.Take(3).ToArray());

        var result = await GetLocationsAsync($"departmentIds={finance},{product}&page={page}");

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(locations.Skip((page - 1) * 5).Take(5), result.Results.Select(location => location.Id));
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111,invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111,")]
    [InlineData(",")]
    public async Task GetLocationList_ShouldReturnBadRequest_WhenDepartmentIdsAreInvalid(string departmentIds)
    {
        var response = await _client.GetAsync($"api/locations?departmentIds={departmentIds}");

        Assert.Equal(400, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("locations.department.ids.invalid", envelope.Error!.Messages[0].Code);
        Assert.Equal("DepartmentIds", envelope.Error.Messages[0].InvalidField);
    }

    private async Task<PagedResult<LocationListItemDto>> GetLocationsAsync(string query)
    {
        var response = await _client.GetAsync($"api/locations?pageSize=5&{query}");
        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<LocationListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        return envelope.Result;
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

    private async Task<Guid> CreateDepartmentAsync(string name, string slug, params Guid[] locationIds)
    {
        var request = new CreateDepartmentRequest(
            Name: name,
            Slug: slug,
            LocationIds: locationIds,
            ParentId: null);

        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);

        return envelope.Result.Id;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
