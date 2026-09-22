using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetAncestorsByIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetAncestorsByIdTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetAncestorsById_ShouldReturnAncestorsFromRootToDirectParent_WhenHierarchyExists()
    {
        var product = await CreateDepartmentAsync("Product", "product");
        var business = await CreateDepartmentAsync("Business", "business");

        var engineering = await CreateDepartmentAsync("Engineering", "engineering", product.Id);
        await CreateDepartmentAsync("Design", "design", product.Id);
        var platform = await CreateDepartmentAsync("Platform", "platform", engineering.Id);
        var backend = await CreateDepartmentAsync("Backend", "backend", platform.Id);
        await CreateDepartmentAsync("Sales", "sales", business.Id);

        var response = await _client.GetAsync(
            $"api/departments/{backend.Id}/ancestors?page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentAncestorDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(3, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);

        Assert.Collection(
            envelope.Result.Results,
            department => AssertDepartment(
                department,
                product.Id,
                "Product",
                "product",
                "product",
                depth: 0,
                parentId: null),
            department => AssertDepartment(
                department,
                engineering.Id,
                "Engineering",
                "engineering",
                "product.engineering",
                depth: 1,
                product.Id),
            department => AssertDepartment(
                department,
                platform.Id,
                "Platform",
                "platform",
                "product.engineering.platform",
                depth: 2,
                engineering.Id));
    }

    [Fact]
    public async Task GetAncestorsById_ShouldReturnEmptyPage_WhenDepartmentIsRoot()
    {
        var department = await CreateDepartmentAsync("Product", "product");

        var response = await _client.GetAsync(
            $"api/departments/{department.Id}/ancestors?page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentAncestorDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(0, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);
        Assert.Empty(envelope.Result.Results);
    }

    [Fact]
    public async Task GetAncestorsById_ShouldReturnBadRequest_WhenPaginationIsInvalid()
    {
        var response = await _client.GetAsync(
            $"api/departments/{Guid.NewGuid()}/ancestors?page=0&pageSize=5");

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("departments.page.invalid", envelope.Error!.Messages[0].Code);
    }

    private static void AssertDepartment(
        DepartmentAncestorDto department,
        Guid expectedId,
        string expectedName,
        string expectedSlug,
        string expectedPath,
        int depth,
        Guid? parentId)
    {
        Assert.Equal(expectedId, department.Id);
        Assert.Equal(expectedName, department.Name);
        Assert.Equal(expectedSlug, department.Slug);
        Assert.Equal(expectedPath, department.Path);
        Assert.Equal(depth, department.Depth);
        Assert.Equal(parentId, department.ParentId);
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(
        string name,
        string slug,
        Guid? parentId = null)
    {
        var request = new CreateDepartmentRequest(
            Name: name,
            Slug: slug,
            LocationIds: [],
            ParentId: parentId);

        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);

        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
