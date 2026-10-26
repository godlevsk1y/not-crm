using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.Locations;
using Shared.Framework.Results;
using Shared.Kernel.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetDepartmentListTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetDepartmentListTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }


    [Theory]
    [InlineData(2)]
    [InlineData(20)]
    public async Task GetDepartmentList_ShouldPreserveTotalCount_WhenPageIsOutOfRange(int page)
    {
        await CreateMockDepartmentsAsync();

        var response = await _client.GetAsync(
            $"api/departments?search=team&sortBy=name&sortDirection=asc&page={page}&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2, envelope.Result.TotalCount);
        Assert.Equal(page, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);
        Assert.Empty(envelope.Result.Results);
    }

    [Fact] 
    public async Task GetDepartmentList_ShouldReturnSelectedDepartments_WhenRequestIsValid()
    {
        await CreateMockDepartmentsAsync();

        var queryParams = new
        {
            Search = "team",
            SortBy = "name",
            SortDirection = "asc",
            Page = 1,
            PageSize = 10,
        };
        
        var response = await _client.GetAsync($"api/departments?" +
                                              $"search={queryParams.Search}" +
                                              $"&sortBy={queryParams.SortBy}" +
                                              $"&sortDirection={queryParams.SortDirection}" +
                                              $"&page={queryParams.Page}" +
                                              $"&pageSize={queryParams.PageSize}");
        
        Assert.Equal(200, (int)response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<DepartmentListItemDto>>>();
        
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        
        Assert.Equal(2, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(10, envelope.Result.PageSize);
        
        Assert.NotEmpty(envelope.Result.Results);
        Assert.Equal("business-team", envelope.Result.Results.FirstOrDefault()!.Slug);
    }
    
    [Fact]
    public async Task GetDepartmentList_ShouldReturnEmptyList_WhenDepartmentsNotFound()
    {
        await CreateMockDepartmentsAsync();
        
        var queryParams = new
        {
            Search = "not exising search query",
            SortBy = "name",
            SortDirection = "asc",
            Page = 1,
            PageSize = 10,
        };
        
        var response = await _client.GetAsync($"api/departments?" +
                                              $"search={queryParams.Search}" +
                                              $"&sortBy={queryParams.SortBy}" +
                                              $"&sortDirection={queryParams.SortDirection}" +
                                              $"&page={queryParams.Page}" +
                                              $"&pageSize={queryParams.PageSize}");
        
        Assert.Equal(200, (int)response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<DepartmentListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        
        Assert.Equal(0, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(10, envelope.Result.PageSize);
        
        Assert.Empty(envelope.Result.Results);
    }

    [Fact]
    public async Task GetDepartmentList_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        var queryParams = new
        {
            Search = "team",
            SortBy = "name",
            SortDirection = "invalid direction",
            Page = 1,
            PageSize = 10,
        };
        
        var response = await _client.GetAsync($"api/departments?" +
                                              $"search={queryParams.Search}" +
                                              $"&sortBy={queryParams.SortBy}" +
                                              $"&sortDirection={queryParams.SortDirection}" +
                                              $"&page={queryParams.Page}" +
                                              $"&pageSize={queryParams.PageSize}");
        
        Assert.Equal(400, (int)response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<DepartmentListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Error);
        Assert.Equal("departments.sort.direction.invalid", envelope.Error.Messages[0].Code);
    }
    
    
    [Theory]
    [InlineData("?isActive=", null)]
    [InlineData("?isActive=true", true)]
    [InlineData("?isActive=false", false)]
    public async Task GetDepartmentList_ShouldFilterByActiveStatus(string queryString, bool? isActive)
    {
        var activeId = await CreateDepartmentAsync("Active Team", "active-team");
        var deletedId = await CreateDepartmentAsync("Deleted Team", "deleted-team");
        var deleteResponse = await _client.DeleteAsync($"api/departments/{deletedId}");
        Assert.Equal(204, (int)deleteResponse.StatusCode);

        var result = await GetDepartmentListAsync(queryString);

        if (isActive is not null)
        {
            Assert.Equal(1, result.TotalCount);
            var department = Assert.Single(result.Results);
            Assert.Equal(isActive.Value ? activeId : deletedId, department.Id);
            Assert.Equal(isActive, department.IsActive);
            Assert.Equal(isActive, department.DeletedAt is null);
        }
        else
        {
            Assert.Equal(2, result.TotalCount);
        }
    }

    [Fact]
    public async Task GetDepartmentList_ShouldReturnAllStatuses_WhenIsActiveIsNull()
    {
        var activeId = await CreateDepartmentAsync("Active Team", "active-team");
        var deletedId = await CreateDepartmentAsync("Deleted Team", "deleted-team");
        var deleteResponse = await _client.DeleteAsync($"api/departments/{deletedId}");
        Assert.Equal(204, (int)deleteResponse.StatusCode);

        var result = await GetDepartmentListAsync("?isActive=");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal([activeId, deletedId], result.Results.Select(d => d.Id));
        Assert.Collection(result.Results,
            department => Assert.True(department.IsActive),
            department => Assert.False(department.IsActive));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDepartmentList_ShouldReturnDepartmentsAtAnySelectedLocation(bool multipleLocations)
    {
        var moscowId = await CreateLocationAsync("Moscow Office", "Moscow");
        var berlinId = await CreateLocationAsync("Berlin Office", "Berlin");
        var parisId = await CreateLocationAsync("Paris Office", "Paris");
        var businessId = await CreateDepartmentAsync("Business Team", "business-team", [moscowId]);
        var productId = await CreateDepartmentAsync("Product Team", "product-team", [berlinId]);
        await CreateDepartmentAsync("Research Team", "research-team", [parisId]);
        await CreateDepartmentAsync("Remote Team", "remote-team");

        var queryString = $"?locationIds={moscowId}";
        if (multipleLocations)
            queryString += $"&locationIds={berlinId}";

        var result = await GetDepartmentListAsync(queryString);

        Guid[] expectedIds = multipleLocations ? [businessId, productId] : [businessId];
        Assert.Equal(expectedIds.Length, result.TotalCount);
        Assert.Equal(expectedIds, result.Results.Select(d => d.Id));
    }

    [Fact]
    public async Task GetDepartmentList_ShouldReturnDepartmentOnce_WhenItHasMultipleSelectedLocations()
    {
        var moscowId = await CreateLocationAsync("Moscow Office", "Moscow");
        var berlinId = await CreateLocationAsync("Berlin Office", "Berlin");
        var departmentId = await CreateDepartmentAsync("Product Team", "product-team", [moscowId, berlinId]);

        var result = await GetDepartmentListAsync($"?locationIds={moscowId}&locationIds={berlinId}");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(departmentId, Assert.Single(result.Results).Id);
    }

    [Fact]
    public async Task GetDepartmentList_ShouldReturnEmptyList_WhenLocationDoesNotMatch()
    {
        var locationId = await CreateLocationAsync("Moscow Office", "Moscow");
        await CreateDepartmentAsync("Product Team", "product-team", [locationId]);
        await CreateDepartmentAsync("Remote Team", "remote-team");

        var result = await GetDepartmentListAsync($"?locationIds={Guid.NewGuid()}");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDepartmentList_ShouldExcludeSelectedDepartments(bool excludeAll)
    {
        var businessId = await CreateDepartmentAsync("Business Team", "business-team");
        var productId = await CreateDepartmentAsync("Product Team", "product-team");
        var researchId = await CreateDepartmentAsync("Research Team", "research-team");

        var queryString = $"?excludeIds={businessId}&excludeIds={researchId}&excludeIds={Guid.NewGuid()}";
        if (excludeAll)
            queryString += $"&excludeIds={productId}";

        var result = await GetDepartmentListAsync(queryString);

        Guid[] expectedIds = excludeAll ? [] : [productId];
        Assert.Equal(expectedIds.Length, result.TotalCount);
        Assert.Equal(expectedIds, result.Results.Select(d => d.Id));
    }

    [Fact]
    public async Task GetDepartmentList_ShouldPreserveDepartments_WhenExcludedIdDoesNotExist()
    {
        var departmentId = await CreateDepartmentAsync("Product Team", "product-team");

        var result = await GetDepartmentListAsync($"?excludeIds={Guid.NewGuid()}");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(departmentId, Assert.Single(result.Results).Id);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetDepartmentList_ShouldApplyAllFiltersBeforePagination(int page)
    {
        var moscowId = await CreateLocationAsync("Moscow Office", "Moscow");
        var berlinId = await CreateLocationAsync("Berlin Office", "Berlin");
        var selectedId = await CreateDepartmentAsync("Product Team", "product-team", [moscowId]);
        var excludedId = await CreateDepartmentAsync("Business Team", "business-team", [moscowId]);
        var deletedId = await CreateDepartmentAsync("Deleted Team", "deleted-team", [moscowId]);
        await CreateDepartmentAsync("Remote Team", "remote-team", [berlinId]);
        await CreateDepartmentAsync("Research Lab", "research-lab", [moscowId]);
        var deleteResponse = await _client.DeleteAsync($"api/departments/{deletedId}");
        Assert.Equal(204, (int)deleteResponse.StatusCode);

        var result = await GetDepartmentListAsync(
            $"?search=team&isActive=true&locationIds={moscowId}&excludeIds={excludedId}&page={page}&pageSize=5");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(5, result.PageSize);
        Guid[] expectedIds = page == 1 ? [selectedId] : [];
        Assert.Equal(expectedIds, result.Results.Select(d => d.Id));
    }

    private async Task<PagedResult<DepartmentListItemDto>> GetDepartmentListAsync(string queryString)
    {
        var response = await _client.GetAsync($"api/departments{queryString}");
        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<DepartmentListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);

        return envelope.Result;
    }

    private async Task<Guid> CreateDepartmentAsync(string name, string slug, Guid[]? locationIds = null)
    {
        var request = new CreateDepartmentRequest(name, slug, locationIds ?? []);
        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);

        return envelope.Result.Id;
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

    private async Task CreateMockDepartmentsAsync()
    {
        CreateDepartmentRequest[] requests =
        [
            new(
                Name: "Product Team",
                Slug: "product-team",
                LocationIds: [],
                ParentId: null
            ),
            new(
                Name: "Business Team",
                Slug: "business-team",
                LocationIds: [],
                ParentId: null
            ),
            new(
                Name: "Research Lab",
                Slug: "research-lab",
                LocationIds: [],
                ParentId: null
            ),
            new(
                Name: "Account Department",
                Slug: "account-department",
                LocationIds: [],
                ParentId: null
            ),
            new(
                Name: "Marketing Office",
                Slug: "marketing-office",
                LocationIds: [],
                ParentId: null
            ),
        ];

        foreach (var request in requests)
        {
            var response = await _client.PostAsJsonAsync("/api/departments", request);
            response.EnsureSuccessStatusCode();
        }
    }
    
    
    
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
