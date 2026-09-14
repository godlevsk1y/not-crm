using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

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