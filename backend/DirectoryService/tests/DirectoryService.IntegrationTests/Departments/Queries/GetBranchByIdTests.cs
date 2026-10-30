using System.Net.Http.Json;
using System.Text.Json;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;
using Shared.Kernel.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetBranchByIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public GetBranchByIdTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetBranchById_ShouldReturnOnlyBranch_WithStoredMetadataAndAllActiveChildCounts()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var company = CreateDepartment(context, "Company", "company");
        var engineering = CreateDepartment(context, "Engineering", "engineering", company);
        var backend = CreateDepartment(context, "Backend", "backend", engineering);
        var api = CreateDepartment(context, "API", "api", backend);
        var payments = CreateDepartment(context, "Payments", "payments", api);
        CreateDepartment(context, "Frontend", "frontend", engineering);
        CreateDepartment(context, "HR", "hr", company);
        CreateDepartment(context, "Archived division", "archived", company).SoftDelete();
        CreateDepartment(context, "Archived team", "archived", engineering).SoftDelete();
        CreateDepartment(context, "Archived backend team", "archived", backend).SoftDelete();

        // Duplicate paths are allowed; these departments must not enter the branch or its counts.
        var otherRoot = CreateDepartment(context, "Other company", "company");
        var otherEngineering = CreateDepartment(context, "Other engineering", "engineering", otherRoot);
        CreateDepartment(context, "Other backend", "backend", otherEngineering);
        await context.SaveChangesAsync();

        await AssertBranchAsync(backend.Id, (company, 2), (engineering, 2), (backend, 1));
        await AssertBranchAsync(company.Id, (company, 2));
        await AssertBranchAsync(payments.Id,
            (company, 2), (engineering, 2), (backend, 1), (api, 1), (payments, 0));
    }

    [Fact]
    public async Task GetBranchById_ShouldReturnSingleNode_WhenRootHasNoActiveChildren()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var root = CreateDepartment(context, "Company", "company");
        CreateDepartment(context, "Archived team", "archived", root).SoftDelete();
        await context.SaveChangesAsync();

        await AssertBranchAsync(root.Id, (root, 0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetBranchById_ShouldReturnNotFound_WhenTargetIsMissingOrDeleted(bool deleted)
    {
        var id = Guid.NewGuid();
        if (deleted)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            var parent = CreateDepartment(context, "Company", "company");
            var target = CreateDepartment(context, "Archived team", "archived", parent);
            target.SoftDelete();
            id = target.Id;
            await context.SaveChangesAsync();
        }

        using var response = await _client.GetAsync($"api/departments/{id}/branch");

        Assert.Equal(404, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("department.not.found", Assert.Single(envelope.Error!.Messages).Code);
    }

    [Fact]
    public async Task GetBranchById_ShouldReturnFullChain_WhenAncestorsExceedMaximumPageSize()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var departments = new List<Department>();
        Department? parent = null;
        for (var i = 0; i < 105; i++)
        {
            parent = CreateDepartment(context, $"Level {i}", $"n{i}", parent);
            departments.Add(parent);
        }
        await context.SaveChangesAsync();

        var targetId = departments[^1].Id.Value;
        await AssertBranchAsync(targetId, departments
            .Select((department, index) => (department, index < departments.Count - 1 ? 1 : 0))
            .ToArray());

        // The existing ancestors endpoint remains paginated and excludes the target.
        using var response = await _client.GetAsync($"api/departments/{targetId}/ancestors");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<DepartmentAncestorDto>>>();
        Assert.NotNull(envelope?.Result);
        Assert.Equal(20, envelope.Result.PageSize);
        Assert.Equal(104, envelope.Result.TotalCount);
        Assert.Equal(departments.Take(20).Select(d => d.Id.Value), envelope.Result.Results.Select(d => d.Id));
    }

    [Fact]
    public async Task GetBranchById_ShouldReflectCurrentHierarchy_AfterSubtreeTransfer()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var company = CreateDepartment(context, "Company", "company");
        var engineering = CreateDepartment(context, "Engineering", "engineering", company);
        var backend = CreateDepartment(context, "Backend", "backend", engineering);
        var otherRoot = CreateDepartment(context, "Other company", "other");
        await context.SaveChangesAsync();

        using var response = await _client.PutAsJsonAsync($"api/departments/{engineering.Id.Value}/parent",
            new TransferDepartmentRequest(otherRoot.Id.Value));
        response.EnsureSuccessStatusCode();
        await context.Entry(engineering).ReloadAsync();
        await context.Entry(backend).ReloadAsync();

        await AssertBranchAsync(backend.Id, (otherRoot, 1), (engineering, 1), (backend, 0));
    }

    [Fact]
    public async Task GetBranchById_ShouldDocumentContractOrderAndResponses_InOpenApi()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("openapi/v1.json"));
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/departments/{id}/branch").GetProperty("get");
        var parameter = Assert.Single(operation.GetProperty("parameters").EnumerateArray());
        Assert.Equal("id", parameter.GetProperty("name").GetString());
        Assert.Equal("path", parameter.GetProperty("in").GetString());
        var description = operation.GetProperty("description").GetString();
        Assert.Contains("root to the selected department", description, StringComparison.Ordinal);
        Assert.Contains("No pagination or depth limit", description, StringComparison.Ordinal);
        var responses = operation.GetProperty("responses");
        var example = responses.GetProperty("200").GetProperty("content")
            .GetProperty("application/json").GetProperty("example");
        Assert.False(example.GetProperty("isError").GetBoolean());
        var nodes = example.GetProperty("result").EnumerateArray().ToArray();
        Assert.Equal(2, nodes.Length);
        Assert.Equal(JsonValueKind.Null, nodes[0].GetProperty("parentId").ValueKind);
        Assert.Equal(nodes[0].GetProperty("id").GetGuid(), nodes[1].GetProperty("parentId").GetGuid());
        Assert.Equal(0, nodes[0].GetProperty("depth").GetInt32());
        Assert.Equal(1, nodes[1].GetProperty("depth").GetInt32());
        Assert.Equal(9, nodes[1].EnumerateObject().Count());
        Assert.Equal("Engineering", nodes[1].GetProperty("name").GetString());
        Assert.Equal("engineering", nodes[1].GetProperty("slug").GetString());
        Assert.Equal("company.engineering", nodes[1].GetProperty("path").GetString());
        Assert.NotEqual(default, nodes[1].GetProperty("createdAt").GetDateTime());
        Assert.Equal(0, nodes[1].GetProperty("childCount").GetInt32());
        Assert.False(nodes[1].GetProperty("hasChildren").GetBoolean());
        Assert.Contains("department.not.found", responses.GetProperty("404").GetRawText(), StringComparison.Ordinal);
        Assert.True(responses.TryGetProperty("500", out _));

        using var health = await _client.GetAsync("health/live");
        Assert.Equal(200, (int)health.StatusCode);
    }

    private async Task AssertBranchAsync(Guid id, params (Department Department, int ChildCount)[] expected)
    {
        using var response = await _client.GetAsync($"api/departments/{id}/branch");
        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<IReadOnlyList<DepartmentBranchNodeDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(expected.Select(item => new DepartmentBranchNodeDto(
            item.Department.Id.Value, item.Department.ParentId?.Value, item.Department.Name.Value,
            item.Department.Slug.Value, item.Department.Path.Value, item.Department.Depth,
            item.Department.CreatedAt, item.ChildCount, item.ChildCount > 0)), envelope.Result);
        Assert.Equal(id, envelope.Result[^1].Id);
    }

    private static Department CreateDepartment(
        DirectoryServiceDbContext context, string name, string slug, Department? parent = null)
    {
        var department = Department.Create(DepartmentName.Create(name).Value, Slug.Create(slug).Value, parent);
        context.Entry(department).Property(d => d.CreatedAt).CurrentValue =
            new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(department.Depth);
        context.Departments.Add(department);
        return department;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
