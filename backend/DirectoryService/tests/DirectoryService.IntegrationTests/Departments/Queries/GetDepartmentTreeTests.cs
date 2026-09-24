using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetDepartmentTreeTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetDepartmentTreeTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetDepartmentTree_ShouldReturnOnlyRootDepartments_WhenHierarchyExists()
    {
        var product = await CreateDepartmentAsync("Product", "product");
        var business = await CreateDepartmentAsync("Business", "business");
        var administration = await CreateDepartmentAsync("Administration", "administration");

        var engineering = await CreateDepartmentAsync("Engineering", "engineering", product.Id);
        await CreateDepartmentAsync("Design", "design", product.Id);
        await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        await CreateDepartmentAsync("Sales", "sales", business.Id);

        var response = await _client.GetAsync("api/departments/tree?page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentNodeDto>>>();
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
                administration.Id,
                "Administration",
                "administration",
                hasChildren: false,
                childCount: 0),
            department => AssertDepartment(
                department,
                business.Id,
                "Business",
                "business",
                hasChildren: true,
                childCount: 1),
            department => AssertDepartment(
                department,
                product.Id,
                "Product",
                "product",
                hasChildren: true,
                childCount: 2));
    }

    [Fact]
    public async Task GetDepartmentTree_ShouldReturnEmptyPage_WhenDepartmentsDoNotExist()
    {
        var response = await _client.GetAsync("api/departments/tree?page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentNodeDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(0, envelope.Result.TotalCount);
        Assert.Empty(envelope.Result.Results);
    }

    [Fact]
    public async Task GetDepartmentTree_ShouldReturnBadRequest_WhenPaginationIsInvalid()
    {
        var response = await _client.GetAsync("api/departments/tree?page=0&pageSize=5");

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("departments.page.invalid", envelope.Error!.Messages[0].Code);
    }

    private static void AssertDepartment(
        DepartmentNodeDto department,
        Guid expectedId,
        string expectedName,
        string expectedSlug,
        bool hasChildren,
        int childCount)
    {
        Assert.Equal(expectedId, department.Id);
        Assert.Equal(expectedName, department.Name);
        Assert.Equal(expectedSlug, department.Slug);
        Assert.Equal(expectedSlug, department.Path);
        Assert.Equal(0, department.Depth);
        Assert.Equal(hasChildren, department.HasChildren);
        Assert.Equal(childCount, department.ChildCount);
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
