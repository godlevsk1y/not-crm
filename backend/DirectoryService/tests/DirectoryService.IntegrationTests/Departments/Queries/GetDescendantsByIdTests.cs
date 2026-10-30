using System.Net.Http.Json;
using System.Text.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using Shared.Framework.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetDescendantsByIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetDescendantsByIdTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetDescendantsById_ShouldReturnOnlyDescendantsOnce_WhenHierarchyHasMultipleLevelsAndBranches()
    {
        var company = await CreateDepartmentAsync("Company", "company");
        var engineering = await CreateDepartmentAsync("Engineering", "engineering", company.Id);
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);
        var payments = await CreateDepartmentAsync("Payments", "payments", api.Id);
        var frontend = await CreateDepartmentAsync("Frontend", "frontend", engineering.Id);
        var web = await CreateDepartmentAsync("Web", "web", frontend.Id);
        var sibling = await CreateDepartmentAsync("Engineering Tools", "engineering-tools", company.Id);
        await CreateDepartmentAsync("Tools", "tools", sibling.Id);
        var otherRoot = await CreateDepartmentAsync("Operations", "operations");
        await CreateDepartmentAsync("Support", "support", otherRoot.Id);

        await AssertDescendantsAsync(engineering.Id, backend, api, payments, frontend, web);
    }

    [Fact]
    public async Task GetDescendantsById_ShouldIncludeArchivedSubtrees_WhenActiveAndArchivedDescendantsExist()
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);
        var frontend = await CreateDepartmentAsync("Frontend", "frontend", engineering.Id);
        await ArchiveDepartmentAsync(api.Id);
        await ArchiveDepartmentAsync(backend.Id);

        await AssertDescendantsAsync(engineering.Id, backend, api, frontend);
        await AssertDescendantsAsync(backend.Id, api);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDescendantsById_ShouldReturnEmptyList_WhenDepartmentHasNoDescendants(bool archived)
    {
        var department = await CreateDepartmentAsync("Engineering", "engineering");
        if (archived)
        {
            await ArchiveDepartmentAsync(department.Id);
        }

        await AssertDescendantsAsync(department.Id);
    }

    [Fact]
    public async Task GetDescendantsById_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var response = await _client.GetAsync($"api/departments/{Guid.NewGuid()}/descendants");

        Assert.Equal(404, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("department.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task GetDescendantsById_ShouldReturnFullList_WhenDescendantsExceedMaximumPageSize()
    {
        var department = await CreateDepartmentAsync("Engineering", "engineering");
        var children = new List<DepartmentDto>();
        for (var i = 0; i < 105; i++)
        {
            children.Add(await CreateDepartmentAsync($"Team {i}", $"team-{i}", department.Id));
        }

        await AssertDescendantsAsync(department.Id, children.ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDescendantsById_ShouldReflectCurrentHierarchy_AfterSubtreeTransfer(bool moveToRoot)
    {
        var engineering = await CreateDepartmentAsync("Engineering", "engineering");
        var operations = await CreateDepartmentAsync("Operations", "operations");
        var backend = await CreateDepartmentAsync("Backend", "backend", engineering.Id);
        var api = await CreateDepartmentAsync("API", "api", backend.Id);
        var payments = await CreateDepartmentAsync("Payments", "payments", api.Id);
        var frontend = await CreateDepartmentAsync("Frontend", "frontend", engineering.Id);
        await ArchiveDepartmentAsync(payments.Id);
        await AssertDescendantsAsync(engineering.Id, backend, api, payments, frontend);

        Guid? newParentId = moveToRoot ? null : operations.Id;
        var response = await _client.PutAsJsonAsync(
            $"api/departments/{backend.Id}/parent", new TransferDepartmentRequest(newParentId));
        Assert.Equal(200, (int)response.StatusCode);

        await AssertDescendantsAsync(engineering.Id, frontend);
        await AssertDescendantsAsync(backend.Id, api, payments);
        await AssertDescendantsAsync(operations.Id, moveToRoot
            ? []
            : [backend with { ParentId = operations.Id }, api, payments]);
    }

    [Fact]
    public async Task GetDescendantsById_ShouldDocumentListAndNotFoundResponse_InOpenApi()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("openapi/v1.json"));
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/departments/{id}/descendants").GetProperty("get");
        var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("id", parameter.GetProperty("name").GetString());
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        var responses = operation.GetProperty("responses");
        var example = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("example");
        Assert.False(example.GetProperty("isError").GetBoolean());
        var descendant = Assert.Single(example.GetProperty("result").EnumerateArray());
        Assert.True(descendant.TryGetProperty("id", out _));
        Assert.True(descendant.TryGetProperty("name", out _));
        Assert.True(descendant.TryGetProperty("parentId", out _));
        Assert.Contains("department.not.found", responses.GetProperty("404").GetRawText(), StringComparison.Ordinal);

        var health = await _client.GetAsync("health/live");
        Assert.Equal(200, (int)health.StatusCode);
    }

    private async Task AssertDescendantsAsync(Guid id, params DepartmentDto[] expected)
    {
        var response = await _client.GetAsync($"api/departments/{id}/descendants");
        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentDescendantDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(
            expected.Select(d => new DepartmentDescendantDto(d.Id, d.Name, d.ParentId)).OrderBy(d => d.Id),
            envelope.Result.OrderBy(d => d.Id));
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(string name, string slug, Guid? parentId = null)
    {
        var response = await _client.PostAsJsonAsync(
            "api/departments", new CreateDepartmentRequest(name, slug, [], parentId));
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        return envelope.Result;
    }

    private async Task ArchiveDepartmentAsync(Guid id)
    {
        var response = await _client.DeleteAsync($"api/departments/{id}");
        Assert.Equal(204, (int)response.StatusCode);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
