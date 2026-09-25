using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Shared.Results;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class TransferDepartmentTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public TransferDepartmentTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task TransferDepartment_ShouldMoveSubtreeUnderNewParent()
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var operations = await CreateDepartmentAsync("Operations", "operations");
        var platform = await CreateDepartmentAsync("Platform", "platform", operations.Id);
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);
        var payments = await CreateDepartmentAsync("Payments", "payments", api.Id);

        var response = await TransferAsync(backend.Id, platform.Id);

        await AssertDepartmentAsync(backend.Id, platform.Id, "operations.platform.backend");
        await AssertDepartmentAsync(api.Id, backend.Id, "operations.platform.backend.api");
        await AssertDepartmentAsync(payments.Id, api.Id, "operations.platform.backend.api.payments");
        await AssertDepartmentAsync(engineering.Id, null, "engineering");
        await AssertDepartmentAsync(platform.Id, operations.Id, "operations.platform");
        await AssertDepthAsync(backend.Id, platform.Id, 2);
        await AssertDepthAsync(api.Id, backend.Id, 3);
        await AssertDepthAsync(payments.Id, api.Id, 4);

        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<TransferredDepartmentDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(backend.Id, envelope.Result.Id);
        Assert.Equal("Backend", envelope.Result.Name);
        Assert.Equal("backend", envelope.Result.Slug);
        Assert.Equal(platform.Id, envelope.Result.ParentId);
        Assert.Equal("operations.platform.backend", envelope.Result.Path);
        Assert.Equal(2, envelope.Result.Depth);

    }

    [Fact]
    public async Task TransferDepartment_ShouldMoveSubtreeToRoot()
    {
        var company = await CreateDepartmentAsync("Company", "company");
        var engineering = await CreateDepartmentAsync("Engineering", "engineering", company.Id);
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);

        var response = await TransferAsync(engineering.Id, null);

        await AssertDepartmentAsync(engineering.Id, null, "engineering");
        await AssertDepartmentAsync(backend.Id, engineering.Id, "engineering.backend");
        await AssertDepartmentAsync(api.Id, backend.Id, "engineering.backend.api");
        await AssertDepartmentAsync(company.Id, null, "company");
        await AssertDepthAsync(engineering.Id, null, 0);
        await AssertDepthAsync(backend.Id, engineering.Id, 1);
        await AssertDepthAsync(api.Id, backend.Id, 2);

        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<TransferredDepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        Assert.Equal(engineering.Id, envelope.Result.Id);
        Assert.Null(envelope.Result.ParentId);
        Assert.Equal("engineering", envelope.Result.Path);
        Assert.Equal(0, envelope.Result.Depth);

    }

    [Fact]
    public async Task TransferDepartment_ShouldLeaveHierarchyUnchanged_WhenParentIsAlreadySet()
    {
        var parent = await CreateDepartmentAsync("Engineering", "engineering");
        var department = await CreateDepartmentAsync("Backend", "backend", parent.Id);
        var child = await CreateDepartmentAsync("API", "api", department.Id);

        var response = await TransferAsync(department.Id, parent.Id);

        await AssertDepartmentAsync(department.Id, parent.Id, "engineering.backend");
        await AssertDepartmentAsync(child.Id, department.Id, "engineering.backend.api");
        await AssertDepthAsync(department.Id, parent.Id, 1);
        await AssertDepthAsync(child.Id, department.Id, 2);

        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<TransferredDepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        Assert.Equal(department.Id, envelope.Result.Id);
        Assert.Equal(parent.Id, envelope.Result.ParentId);
        Assert.Equal("engineering.backend", envelope.Result.Path);
        Assert.Equal(1, envelope.Result.Depth);

    }

    [Fact]
    public async Task TransferDepartment_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var parent = await CreateDepartmentAsync("Engineering", "engineering");

        var response = await TransferAsync(Guid.NewGuid(), parent.Id);

        await AssertErrorAsync(response, 404, "department.not.found");
    }

    [Fact]
    public async Task TransferDepartment_ShouldReturnNotFound_WhenNewParentDoesNotExist()
    {
        var department = await CreateDepartmentAsync("Backend", "backend");

        var response = await TransferAsync(department.Id, Guid.NewGuid());

        await AssertErrorAsync(response, 404, "department.parent.not.found");
        await AssertDepartmentAsync(department.Id, null, "backend");
    }

    [Fact]
    public async Task TransferDepartment_ShouldReturnBadRequest_WhenDepartmentIdIsEmpty()
    {
        var response = await TransferAsync(Guid.Empty, null);

        await AssertErrorAsync(response, 400, "department.id.empty");
    }

    [Fact]
    public async Task TransferDepartment_ShouldReturnConflict_WhenNewParentIsSelf()
    {
        var department = await CreateDepartmentAsync("Engineering", "engineering");

        var response = await TransferAsync(department.Id, department.Id);

        await AssertErrorAsync(response, 409, "department.transfer.parent_to_self");
        await AssertDepartmentAsync(department.Id, null, "engineering");
    }

    [Fact]
    public async Task TransferDepartment_ShouldReturnConflict_WhenNewParentIsDescendant()
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);

        var response = await TransferAsync(engineering.Id, api.Id);

        await AssertErrorAsync(response, 409, "department.transfer.cycle");
        await AssertDepartmentAsync(engineering.Id, null, "engineering");
        await AssertDepartmentAsync(backend.Id, engineering.Id, "engineering.backend");
        await AssertDepartmentAsync(api.Id, backend.Id, "engineering.backend.api");
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(string name, string slug, Guid? parentId = null)
    {
        var request = new CreateDepartmentRequest(name, slug, [], parentId);
        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        return envelope.Result;
    }

    private Task<HttpResponseMessage> TransferAsync(Guid departmentId, Guid? newParentId) =>
        _client.PutAsJsonAsync(
            $"api/departments/{departmentId}/parent",
            new TransferDepartmentRequest(newParentId));

    private async Task AssertDepartmentAsync(Guid id, Guid? parentId, string path)
    {
        var response = await _client.GetAsync($"api/departments/{id}");
        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        Assert.Equal(id, envelope.Result.Id);
        Assert.Equal(parentId, envelope.Result.ParentId);
        Assert.Equal(path, envelope.Result.Path);
    }

    private async Task AssertDepthAsync(Guid id, Guid? parentId, int expectedDepth)
    {
        var url = parentId is null
            ? "api/departments/tree"
            : $"api/departments/{parentId}/children";
        var response = await _client.GetAsync(url);
        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content
            .ReadFromJsonAsync<Envelope<PagedResult<DepartmentNodeDto>>>();
        Assert.NotNull(envelope?.Result);
        var department = Assert.Single(envelope.Result.Results, department => department.Id == id);
        Assert.Equal(expectedDepth, department.Depth);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, int statusCode, string code)
    {
        Assert.Equal(statusCode, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal(code, envelope.Error!.Messages[0].Code);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
