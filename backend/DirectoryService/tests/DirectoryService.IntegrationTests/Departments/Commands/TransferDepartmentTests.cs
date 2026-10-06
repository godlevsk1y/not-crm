using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using Shared.Framework.Results;
using Shared.Kernel.Results;

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

    [Fact]
    public async Task TransferDepartment_ShouldKeepSubtreeConsistent_WhenSameDepartmentIsMovedConcurrently()
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var operations = await CreateDepartmentAsync("Operations", "operations");
        var platform = await CreateDepartmentAsync("Platform", "platform", operations.Id);
        var backend = await CreateDepartmentAsync("Backend", "backend");
        var api = await CreateDepartmentAsync("API", "api", backend.Id);
        var payments = await CreateDepartmentAsync("Payments", "payments", api.Id);

        var responses = await Task.WhenAll(
            TransferAsync(backend.Id, engineering.Id),
            TransferAsync(backend.Id, platform.Id));

        await AssertConcurrentTransferResponsesAsync(responses);

        var response = await _client.GetAsync($"api/departments/{backend.Id}");
        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        var finalParentId = envelope.Result.ParentId;
        Assert.True(finalParentId == engineering.Id || finalParentId == platform.Id);

        if ((int)responses[0].StatusCode == 409)
        {
            Assert.Equal(platform.Id, finalParentId);
        }
        else if ((int)responses[1].StatusCode == 409)
        {
            Assert.Equal(engineering.Id, finalParentId);
        }

        var expectedPath = finalParentId == engineering.Id
            ? "engineering.backend"
            : "operations.platform.backend";
        var expectedDepth = finalParentId == engineering.Id ? 1 : 2;

        await AssertDepartmentAsync(backend.Id, finalParentId, expectedPath);
        await AssertDepartmentAsync(api.Id, backend.Id, $"{expectedPath}.api");
        await AssertDepartmentAsync(payments.Id, api.Id, $"{expectedPath}.api.payments");
        await AssertDepthAsync(backend.Id, finalParentId, expectedDepth);
        await AssertDepthAsync(api.Id, backend.Id, expectedDepth + 1);
        await AssertDepthAsync(payments.Id, api.Id, expectedDepth + 2);
        await AssertDepartmentAsync(engineering.Id, null, "engineering");
        await AssertDepartmentAsync(operations.Id, null, "operations");
        await AssertDepartmentAsync(platform.Id, operations.Id, "operations.platform");
    }

    [Fact]
    public async Task TransferDepartment_ShouldRejectCycle_WhenDepartmentsAreMovedUnderEachOtherConcurrently()
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var operations = await CreateDepartmentAsync("Operations", "operations");
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var platform = await CreateDepartmentAsync("Platform", "platform", operations.Id);

        var responses = await Task.WhenAll(
            TransferAsync(engineering.Id, operations.Id),
            TransferAsync(operations.Id, engineering.Id));

        Assert.Single(responses, response => (int)response.StatusCode == 200);
        var conflict = Assert.Single(responses, response => (int)response.StatusCode == 409);
        await AssertErrorAsync(
            conflict, 409, "department.transfer.cycle", "department.transfer.conflict");

        if ((int)responses[0].StatusCode == 200)
        {
            await AssertDepartmentAsync(operations.Id, null, "operations");
            await AssertDepartmentAsync(engineering.Id, operations.Id, "operations.engineering");
            await AssertDepartmentAsync(backend.Id, engineering.Id, "operations.engineering.backend");
            await AssertDepartmentAsync(platform.Id, operations.Id, "operations.platform");
            await AssertDepthAsync(operations.Id, null, 0);
            await AssertDepthAsync(engineering.Id, operations.Id, 1);
            await AssertDepthAsync(backend.Id, engineering.Id, 2);
            await AssertDepthAsync(platform.Id, operations.Id, 1);
        }
        else
        {
            await AssertDepartmentAsync(engineering.Id, null, "engineering");
            await AssertDepartmentAsync(operations.Id, engineering.Id, "engineering.operations");
            await AssertDepartmentAsync(backend.Id, engineering.Id, "engineering.backend");
            await AssertDepartmentAsync(platform.Id, operations.Id, "engineering.operations.platform");
            await AssertDepthAsync(engineering.Id, null, 0);
            await AssertDepthAsync(operations.Id, engineering.Id, 1);
            await AssertDepthAsync(backend.Id, engineering.Id, 1);
            await AssertDepthAsync(platform.Id, operations.Id, 2);
        }
    }

    [Fact]
    public async Task TransferDepartment_ShouldKeepSubtreesConsistent_WhenDepartmentAndParentAreMovedConcurrently()
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var operations = await CreateDepartmentAsync("Operations", "operations");
        var platform = await CreateDepartmentAsync("Platform", "platform", operations.Id);
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);
        var payments = await CreateDepartmentAsync("Payments", "payments", api.Id);
        var frontend = await CreateDepartmentAsync("Frontend", "frontend", engineering.Id);

        var responses = await Task.WhenAll(
            TransferAsync(backend.Id, platform.Id),
            TransferAsync(engineering.Id, operations.Id));

        await AssertConcurrentTransferResponsesAsync(responses);

        var backendMoved = (int)responses[0].StatusCode == 200;
        var engineeringMoved = (int)responses[1].StatusCode == 200;
        Guid? engineeringParentId = engineeringMoved ? operations.Id : null;
        var engineeringPath = engineeringMoved ? "operations.engineering" : "engineering";
        var engineeringDepth = engineeringMoved ? 1 : 0;
        var backendParentId = backendMoved ? platform.Id : engineering.Id;
        var backendPath = backendMoved
            ? "operations.platform.backend"
            : $"{engineeringPath}.backend";
        var backendDepth = backendMoved ? 2 : engineeringDepth + 1;

        await AssertDepartmentAsync(engineering.Id, engineeringParentId, engineeringPath);
        await AssertDepartmentAsync(frontend.Id, engineering.Id, $"{engineeringPath}.frontend");
        await AssertDepartmentAsync(backend.Id, backendParentId, backendPath);
        await AssertDepartmentAsync(api.Id, backend.Id, $"{backendPath}.api");
        await AssertDepartmentAsync(payments.Id, api.Id, $"{backendPath}.api.payments");
        await AssertDepthAsync(engineering.Id, engineeringParentId, engineeringDepth);
        await AssertDepthAsync(frontend.Id, engineering.Id, engineeringDepth + 1);
        await AssertDepthAsync(backend.Id, backendParentId, backendDepth);
        await AssertDepthAsync(api.Id, backend.Id, backendDepth + 1);
        await AssertDepthAsync(payments.Id, api.Id, backendDepth + 2);
        await AssertDepartmentAsync(operations.Id, null, "operations");
        await AssertDepartmentAsync(platform.Id, operations.Id, "operations.platform");
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

    private static async Task AssertConcurrentTransferResponsesAsync(HttpResponseMessage[] responses)
    {
        Assert.Contains(responses, response => (int)response.StatusCode == 200);
        Assert.All(responses, response =>
            Assert.True((int)response.StatusCode is 200 or 409,
                $"Expected HTTP 200 or 409, got {(int)response.StatusCode}."));

        foreach (var response in responses.Where(response => (int)response.StatusCode == 409))
        {
            await AssertErrorAsync(response, 409, "department.transfer.conflict");
        }
    }

    private static async Task AssertErrorAsync(
        HttpResponseMessage response, int statusCode, params string[] codes)
    {
        Assert.Equal(statusCode, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Error);
        var message = Assert.Single(envelope.Error.Messages);
        Assert.Contains(message.Code, codes);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
