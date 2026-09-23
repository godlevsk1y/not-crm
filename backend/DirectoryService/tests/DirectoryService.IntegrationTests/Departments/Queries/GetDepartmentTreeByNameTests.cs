using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetDepartmentTreeByNameTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetDepartmentTreeByNameTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetDepartmentTreeByName_ShouldReturnMatchingDepartmentsWithAncestorsFromRootToParent()
    {
        var product = await CreateDepartmentAsync("Product", "product");
        var business = await CreateDepartmentAsync("Business", "business");
        var engineering = await CreateDepartmentAsync("Engineering", "engineering", product.Id);
        var platform = await CreateDepartmentAsync("Platform", "platform", engineering.Id);
        var platformSales = await CreateDepartmentAsync("Platform Sales", "platform-sales", business.Id);
        await CreateDepartmentAsync("Design", "design", product.Id);

        var result = await GetResultsAsync("platform");

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Collection(result.Results,
            department =>
            {
                AssertDepartment(department, platform.Id, "Platform", "platform",
                    "product.engineering.platform", 2, engineering.Id);
                Assert.Collection(department.Ancestors,
                    ancestor => AssertAncestor(ancestor, product.Id, "Product", "product",
                        "product", 0, null),
                    ancestor => AssertAncestor(ancestor, engineering.Id, "Engineering", "engineering",
                        "product.engineering", 1, product.Id));
            },
            department =>
            {
                AssertDepartment(department, platformSales.Id, "Platform Sales", "platform-sales",
                    "business.platform-sales", 1, business.Id);
                Assert.Collection(department.Ancestors,
                    ancestor => AssertAncestor(ancestor, business.Id, "Business", "business",
                        "business", 0, null));
            });
    }

    [Fact]
    public async Task GetDepartmentTreeByName_ShouldReturnRootWithoutAncestors_WhenRootMatches()
    {
        var product = await CreateDepartmentAsync("Product", "product");
        await CreateDepartmentAsync("Engineering", "engineering", product.Id);

        var result = await GetResultsAsync("PRODUCT");

        Assert.Equal(1, result.TotalCount);
        var department = Assert.Single(result.Results);
        AssertDepartment(department, product.Id, "Product", "product", "product", 0, null);
        Assert.Empty(department.Ancestors);
    }

    [Fact]
    public async Task GetDepartmentTreeByName_ShouldReturnEmptyPage_WhenNothingMatches()
    {
        await CreateDepartmentAsync("Product", "product");

        var result = await GetResultsAsync("missing");

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task GetDepartmentTreeByName_ShouldExcludeDeletedDepartments()
    {
        var deleted = await CreateDepartmentAsync("Team Old", "team-old");
        var active = await CreateDepartmentAsync("Team New", "team-new");
        var deleteResponse = await _client.DeleteAsync($"api/departments/{deleted.Id}");
        Assert.Equal(204, (int)deleteResponse.StatusCode);

        var result = await GetResultsAsync("team");

        Assert.Equal(1, result.TotalCount);
        var department = Assert.Single(result.Results);
        Assert.Equal(active.Id, department.Id);
    }

    [Fact]
    public async Task GetDepartmentTreeByName_ShouldPaginateDepartmentsRatherThanAncestors()
    {
        var root = await CreateDepartmentAsync("Root", "root");
        var parent = await CreateDepartmentAsync("Parent", "parent", root.Id);
        for (var number = 1; number <= 6; number++)
        {
            await CreateDepartmentAsync($"Team {number}", $"team-{number}", parent.Id);
        }

        var result = await GetResultsAsync("team", page: 2, pageSize: 5);

        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        var department = Assert.Single(result.Results);
        Assert.Equal("Team 6", department.Name);
        Assert.Collection(department.Ancestors,
            ancestor => AssertAncestor(ancestor, root.Id, "Root", "root", "root", 0, null),
            ancestor => AssertAncestor(ancestor, parent.Id, "Parent", "parent",
                "root.parent", 1, root.Id));
    }

    [Theory]
    [InlineData("a", 1, 5, "departments.search.query.invalid")]
    [InlineData("team", 0, 5, "departments.page.invalid")]
    [InlineData("team", 1, 4, "departments.page.size.invalid")]
    public async Task GetDepartmentTreeByName_ShouldReturnBadRequest_WhenQueryIsInvalid(
        string search, int page, int pageSize, string errorCode)
    {
        var response = await _client.GetAsync(
            $"api/departments/tree/search?q={search}&page={page}&pageSize={pageSize}");

        Assert.Equal(400, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal(errorCode, envelope.Error!.Messages[0].Code);
    }

    private async Task<PagedResult<DepartmentWithAncestorsDto>> GetResultsAsync(
        string search, int page = 1, int pageSize = 20)
    {
        var response = await _client.GetAsync(
            $"api/departments/tree/search?q={search}&page={page}&pageSize={pageSize}");

        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentWithAncestorsDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        return envelope.Result;
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(
        string name, string slug, Guid? parentId = null)
    {
        var request = new CreateDepartmentRequest(name, slug, [], parentId);
        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        return envelope.Result;
    }

    private static void AssertDepartment(
        DepartmentWithAncestorsDto department, Guid id, string name, string slug,
        string path, int depth, Guid? parentId)
    {
        Assert.Equal(id, department.Id);
        Assert.Equal(name, department.Name);
        Assert.Equal(slug, department.Slug);
        Assert.Equal(path, department.Path);
        Assert.Equal(depth, department.Depth);
        Assert.Equal(parentId, department.ParentId);
    }

    private static void AssertAncestor(
        DepartmentAncestorDto ancestor, Guid id, string name, string slug,
        string path, int depth, Guid? parentId)
    {
        Assert.Equal(id, ancestor.Id);
        Assert.Equal(name, ancestor.Name);
        Assert.Equal(slug, ancestor.Slug);
        Assert.Equal(path, ancestor.Path);
        Assert.Equal(depth, ancestor.Depth);
        Assert.Equal(parentId, ancestor.ParentId);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
