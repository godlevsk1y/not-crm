using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using Shared.Kernel.Results;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class UpdateDepartmentTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public UpdateDepartmentTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task UpdateDepartment_ShouldSucceed_WhenRequestIsValid()
    {
        var parent = await CreateDepartmentAsync("Engineering", "engineering");
        var department = await CreateDepartmentAsync("Backend Team", "backend-team");
        var request = new UpdateDepartmentRequest(
            Name: "Platform Team",
            Slug: "platform-team",
            ParentId: parent.Id
        );

        var response = await _client.PatchAsJsonAsync($"api/departments/{department.Id}", request);

        Assert.Equal(200, (int)response.StatusCode);

        var updateEnvelope = await response.Content.ReadFromJsonAsync<Envelope<Guid>>();
        Assert.NotNull(updateEnvelope);
        Assert.False(updateEnvelope.IsError);
        Assert.Equal(department.Id, updateEnvelope.Result);

        var getResponse = await _client.GetAsync($"api/departments/{department.Id}");
        var getEnvelope = await getResponse.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();

        Assert.NotNull(getEnvelope);
        Assert.False(getEnvelope.IsError);
        Assert.NotNull(getEnvelope.Result);
        Assert.Equal("Platform Team", getEnvelope.Result.Name);
        Assert.Equal("platform-team", getEnvelope.Result.Slug);
        Assert.Equal("engineering.platform-team", getEnvelope.Result.Path);
        Assert.Equal(parent.Id, getEnvelope.Result.ParentId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateDepartment_ShouldUpdateDescendantPaths_WhenSlugChanges(bool isNested)
    {
        var root = isNested ? await CreateDepartmentAsync("Company", "company") : null;
        var department = await CreateDepartmentAsync("Engineering", "engineering", root?.Id);
        var child = await CreateDepartmentAsync("Backend", "backend", department.Id);
        var grandchild = await CreateDepartmentAsync("Services", "services", child.Id);
        var sibling = await CreateDepartmentAsync("Engineering Tools", "engineering-tools", root?.Id);
        var unrelated = await CreateDepartmentAsync("Tools Child", "tools-child", sibling.Id);
        var prefix = isNested ? "company.platform" : "platform";

        var response = await _client.PatchAsJsonAsync(
            $"api/departments/{department.Id}",
            new UpdateDepartmentRequest(Name: null, Slug: "platform", ParentId: null));

        Assert.Equal(200, (int)response.StatusCode);
        await AssertDepartmentPathAsync(department.Id, prefix, root?.Id);
        await AssertDepartmentPathAsync(child.Id, $"{prefix}.backend", department.Id);
        await AssertDepartmentPathAsync(grandchild.Id, $"{prefix}.backend.services", child.Id);
        await AssertDepartmentPathAsync(unrelated.Id, unrelated.Path, sibling.Id);

        var treeUrl = root is null
            ? "api/departments/tree?page=1&pageSize=20"
            : $"api/departments/{root.Id}/children?page=1&pageSize=20";
        var tree = await _client.GetFromJsonAsync<Envelope<PagedResult<DepartmentNodeDto>>>(treeUrl);
        Assert.NotNull(tree?.Result);
        var node = Assert.Single(tree.Result.Results, d => d.Id == department.Id);
        Assert.Equal(isNested ? 1 : 0, node.Depth);
        Assert.True(node.HasChildren);
        Assert.Equal(1, node.ChildCount);

        var children = await _client.GetFromJsonAsync<Envelope<PagedResult<DepartmentNodeDto>>>(
            $"api/departments/{department.Id}/children?page=1&pageSize=20");
        Assert.NotNull(children?.Result);
        var childNode = Assert.Single(children.Result.Results);
        Assert.Equal(child.Id, childNode.Id);
        Assert.Equal(isNested ? 2 : 1, childNode.Depth);
        Assert.True(childNode.HasChildren);
        Assert.Equal(1, childNode.ChildCount);

        var ancestors = await _client.GetFromJsonAsync<Envelope<PagedResult<DepartmentAncestorDto>>>(
            $"api/departments/{grandchild.Id}/ancestors?page=1&pageSize=20");
        Assert.NotNull(ancestors?.Result);
        Guid[] expectedIds = root is null
            ? [department.Id, child.Id]
            : [root.Id, department.Id, child.Id];
        Assert.Equal(expectedIds, ancestors.Result.Results.Select(d => d.Id));
        Assert.Equal(prefix, ancestors.Result.Results.Single(d => d.Id == department.Id).Path);
        Assert.Equal($"{prefix}.backend", ancestors.Result.Results.Single(d => d.Id == child.Id).Path);
    }

    [Fact]
    public async Task UpdateDepartment_ShouldRollBackSlugChange_WhenDescendantUpdateFails()
    {
        var department = await CreateDepartmentAsync("Engineering", "engineering");
        var child = await CreateDepartmentAsync("Backend", "backend", department.Id);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("""
            ALTER TABLE departments ADD CONSTRAINT reject_descendant_path
            CHECK (path <> 'platform.backend'::ltree);
            """);

        try
        {
            var response = await _client.PatchAsJsonAsync(
                $"api/departments/{department.Id}",
                new UpdateDepartmentRequest(Name: null, Slug: "platform", ParentId: null));

            Assert.Equal(500, (int)response.StatusCode);
            await AssertDepartmentPathAsync(department.Id, "engineering", parentId: null);
            await AssertDepartmentPathAsync(child.Id, "engineering.backend", department.Id);
            var envelope = await _client.GetFromJsonAsync<Envelope<DepartmentDto>>(
                $"api/departments/{department.Id}");
            Assert.NotNull(envelope?.Result);
            Assert.Equal("engineering", envelope.Result.Slug);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE departments DROP CONSTRAINT reject_descendant_path;");
        }
    }

    private async Task AssertDepartmentPathAsync(Guid id, string path, Guid? parentId)
    {
        var envelope = await _client.GetFromJsonAsync<Envelope<DepartmentDto>>($"api/departments/{id}");
        Assert.NotNull(envelope?.Result);
        Assert.Equal(path, envelope.Result.Path);
        Assert.Equal(parentId, envelope.Result.ParentId);
    }

    [Fact]
    public async Task UpdateDepartment_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var departmentId = Guid.NewGuid();
        var request = new UpdateDepartmentRequest(
            Name: "Platform Team",
            Slug: null,
            ParentId: null
        );

        var response = await _client.PatchAsJsonAsync($"api/departments/{departmentId}", request);

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("department.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task UpdateDepartment_ShouldReturnBadRequest_WhenSlugIsInvalid()
    {
        var department = await CreateDepartmentAsync("Backend Team", "backend-team");
        var request = new UpdateDepartmentRequest(
            Name: null,
            Slug: "INVALID SLUG",
            ParentId: null
        );

        var response = await _client.PatchAsJsonAsync($"api/departments/{department.Id}", request);

        Assert.Equal(400, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("slug.invalid", envelope.Error!.Messages[0].Code);
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(string name, string slug, Guid? parentId = null)
    {
        var request = new CreateDepartmentRequest(
            Name: name,
            Slug: slug,
            LocationIds: [],
            ParentId: parentId
        );

        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Result);

        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
