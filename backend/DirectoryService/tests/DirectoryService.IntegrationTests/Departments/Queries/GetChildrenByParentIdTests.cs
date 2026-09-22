using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetChildrenByParentIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetChildrenByParentIdTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetChildrenByParentId_ShouldReturnOnlyDirectChildren_WhenHierarchyExists()
    {
        var product = await CreateDepartmentAsync("Product", "product");
        var business = await CreateDepartmentAsync("Business", "business");

        var engineering = await CreateDepartmentAsync("Engineering", "engineering", product.Id);
        var design = await CreateDepartmentAsync("Design", "design", product.Id);
        await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        await CreateDepartmentAsync("Sales", "sales", business.Id);

        var response = await _client.GetAsync(
            $"api/departments/{product.Id}/children?page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentTreeItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(2, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);

        Assert.Collection(
            envelope.Result.Results,
            department => AssertDepartment(
                department,
                design.Id,
                "Design",
                "design",
                "product.design",
                hasChildren: false,
                childCount: 0),
            department => AssertDepartment(
                department,
                engineering.Id,
                "Engineering",
                "engineering",
                "product.engineering",
                hasChildren: true,
                childCount: 1));
    }

    [Fact]
    public async Task GetChildrenByParentId_ShouldReturnEmptyPage_WhenDepartmentHasNoChildren()
    {
        var department = await CreateDepartmentAsync("Product", "product");

        var response = await _client.GetAsync(
            $"api/departments/{department.Id}/children?page=1&pageSize=5");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentTreeItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(0, envelope.Result.TotalCount);
        Assert.Equal(1, envelope.Result.Page);
        Assert.Equal(5, envelope.Result.PageSize);
        Assert.Empty(envelope.Result.Results);
    }

    [Fact]
    public async Task GetChildrenByParentId_ShouldReturnBadRequest_WhenPaginationIsInvalid()
    {
        var response = await _client.GetAsync(
            $"api/departments/{Guid.NewGuid()}/children?page=0&pageSize=5");

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("departments.page.invalid", envelope.Error!.Messages[0].Code);
    }

    private static void AssertDepartment(
        DepartmentTreeItemDto department,
        Guid expectedId,
        string expectedName,
        string expectedSlug,
        string expectedPath,
        bool hasChildren,
        int childCount)
    {
        Assert.Equal(expectedId, department.Id);
        Assert.Equal(expectedName, department.Name);
        Assert.Equal(expectedSlug, department.Slug);
        Assert.Equal(expectedPath, department.Path);
        Assert.Equal(1, department.Depth);
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
