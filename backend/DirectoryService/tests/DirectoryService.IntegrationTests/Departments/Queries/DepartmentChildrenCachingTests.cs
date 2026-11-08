using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Departments.QueryContracts;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Departments.Caching;
using DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Framework.Results;
using Shared.Kernel.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class DepartmentChildrenCachingTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public DepartmentChildrenCachingTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Children_ShouldCachePagesAndEmptyResults_WithoutOpeningDatabaseConnections()
    {
        var root = await CreateAsync("Root", "root");
        await CreateAsync("Child", "child", root.Id);
        var missingId = Guid.NewGuid();

        await ChildrenAsync(root.Id);
        await ChildrenAsync(root.Id, page: 2);
        await ChildrenAsync(root.Id, pageSize: 10);
        await ChildrenAsync(missingId);
        var connections = _factory.OpenedReadConnections;

        Assert.Single((await ChildrenAsync(root.Id)).Results);
        Assert.Empty((await ChildrenAsync(root.Id, page: 2)).Results);
        Assert.Equal(10, (await ChildrenAsync(root.Id, pageSize: 10)).PageSize);
        Assert.Empty((await ChildrenAsync(missingId)).Results);
        Assert.Equal(connections, _factory.OpenedReadConnections);
    }

    [Fact]
    public async Task Children_ShouldDeserializePageFromRedis_InAnotherCacheInstance()
    {
        var root = await CreateAsync("Root", "root");
        var child = await CreateAsync("Child", "child", root.Id);
        await ChildrenAsync(root.Id);

        var redisOptions = _factory.Services.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        var services = new ServiceCollection();
        services.AddStackExchangeRedisCache(options => options.Configuration = redisOptions.Configuration);
        services.AddHybridCache();
        services.AddSingleton<DepartmentChildrenCache>();
        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<DepartmentChildrenCache>();

        var page = await cache.GetOrCreateAsync(
            new GetChildrenByParentIdQuery(root.Id, PageSize: 5),
            _ => throw new InvalidOperationException("Expected a Redis cache hit"),
            CancellationToken.None);

        Assert.Equal(child.Id, Assert.Single(page.Results).Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Update_ShouldInvalidateSubtreeAndParent_WhilePreservingAnotherBranch(bool changeSlug)
    {
        var root = await CreateAsync("Root", "root");
        var branch = await CreateAsync("Branch", "branch", root.Id);
        var child = await CreateAsync("Child", "child", branch.Id);
        var leaf = await CreateAsync("Leaf", "leaf", child.Id);
        var sibling = await CreateAsync("Sibling", "sibling", root.Id);
        await CreateAsync("Other", "other", sibling.Id);
        Guid[] affectedParents = [root.Id, branch.Id, child.Id, leaf.Id];
        foreach (var id in affectedParents)
        {
            await ChildrenAsync(id);
        }
        await ChildrenAsync(sibling.Id);

        var response = await _client.PatchAsJsonAsync($"departments/{branch.Id}",
            changeSlug ? new UpdateDepartmentRequest(null, "renamed") : new UpdateDepartmentRequest("Renamed", null));
        response.EnsureSuccessStatusCode();
        var connections = _factory.OpenedReadConnections;

        var parentPage = await ChildrenAsync(root.Id);
        var branchNode = Assert.Single(parentPage.Results, node => node.Id == branch.Id);
        Assert.Equal(changeSlug ? "Branch" : "Renamed", branchNode.Name);
        Assert.Equal(changeSlug ? "root.renamed" : "root.branch", branchNode.Path);
        Assert.Equal(changeSlug ? "root.renamed.child" : "root.branch.child",
            Assert.Single((await ChildrenAsync(branch.Id)).Results).Path);
        Assert.Equal(changeSlug ? "root.renamed.child.leaf" : "root.branch.child.leaf",
            Assert.Single((await ChildrenAsync(child.Id)).Results).Path);
        Assert.Empty((await ChildrenAsync(leaf.Id)).Results);
        await ChildrenAsync(sibling.Id);
        Assert.Equal(connections + affectedParents.Length, _factory.OpenedReadConnections);
    }

    [Fact]
    public async Task Rename_ShouldInvalidateEveryPageAndPageSize_WhenOrderingChanges()
    {
        var root = await CreateAsync("Root", "root");
        for (var i = 0; i < 5; i++)
        {
            await CreateAsync($"Team {i}", $"team-{i}", root.Id);
        }
        var last = await CreateAsync("Zulu", "zulu", root.Id);
        await ChildrenAsync(root.Id);
        await ChildrenAsync(root.Id, page: 2);
        await ChildrenAsync(root.Id, pageSize: 10);

        var response = await _client.PatchAsJsonAsync($"departments/{last.Id}",
            new UpdateDepartmentRequest("Alpha", null));
        response.EnsureSuccessStatusCode();
        var connections = _factory.OpenedReadConnections;

        Assert.Equal(last.Id, (await ChildrenAsync(root.Id)).Results.First().Id);
        Assert.DoesNotContain((await ChildrenAsync(root.Id, page: 2)).Results, node => node.Id == last.Id);
        Assert.Equal(last.Id, (await ChildrenAsync(root.Id, pageSize: 10)).Results.First().Id);
        Assert.Equal(connections + 3, _factory.OpenedReadConnections);
    }

    [Fact]
    public async Task Create_ShouldInvalidateParentAndGrandparent_WhilePreservingAnotherBranch()
    {
        var root = await CreateAsync("Root", "root");
        var branch = await CreateAsync("Branch", "branch", root.Id);
        var sibling = await CreateAsync("Sibling", "sibling", root.Id);
        var other = await CreateAsync("Other", "other", sibling.Id);
        var branchNode = Assert.Single((await ChildrenAsync(root.Id)).Results, node => node.Id == branch.Id);
        Assert.Equal(0, branchNode.ChildCount);
        Assert.False(branchNode.HasChildren);
        Assert.Empty((await ChildrenAsync(branch.Id)).Results);
        await ChildrenAsync(sibling.Id);

        var child = await CreateAsync("New child", "new-child", branch.Id);
        var connections = _factory.OpenedReadConnections;

        var branchPage = await ChildrenAsync(branch.Id);
        Assert.Equal(child.Id, Assert.Single(branchPage.Results).Id);
        Assert.Equal(1, branchPage.TotalCount);
        Assert.Equal(connections + 1, _factory.OpenedReadConnections);

        var rootPage = await ChildrenAsync(root.Id);
        branchNode = Assert.Single(rootPage.Results, node => node.Id == branch.Id);
        Assert.Equal(1, branchNode.ChildCount);
        Assert.True(branchNode.HasChildren);
        Assert.Equal(2, rootPage.TotalCount);
        Assert.Equal(connections + 2, _factory.OpenedReadConnections);

        Assert.Equal(other.Id, Assert.Single((await ChildrenAsync(sibling.Id)).Results).Id);
        Assert.Equal(connections + 2, _factory.OpenedReadConnections);
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("restore")]
    [InlineData("transfer")]
    public async Task StructureChange_ShouldInvalidateAllChildrenLists(string operation)
    {
        var root = await CreateAsync("Root", "root");
        var branch = await CreateAsync("Branch", "branch", root.Id);
        var destination = await CreateAsync("Destination", "destination");
        var unrelated = await CreateAsync("Unrelated", "unrelated");
        if (string.Equals(operation, "restore", StringComparison.Ordinal))
        {
            (await _client.DeleteAsync($"departments/{branch.Id}")).EnsureSuccessStatusCode();
        }
        await ChildrenAsync(root.Id);
        await ChildrenAsync(destination.Id);
        await ChildrenAsync(unrelated.Id);

        using var response = operation switch
        {
            "delete" => await _client.DeleteAsync($"departments/{branch.Id}"),
            "restore" => await _client.PostAsync($"departments/{branch.Id}/restore", null),
            _ => await _client.PutAsJsonAsync($"departments/{branch.Id}/parent",
                new TransferDepartmentRequest(destination.Id)),
        };
        response.EnsureSuccessStatusCode();
        var connections = _factory.OpenedReadConnections;

        var sourcePage = await ChildrenAsync(root.Id);
        var destinationPage = await ChildrenAsync(destination.Id);
        await ChildrenAsync(unrelated.Id);
        Assert.Equal(connections + 3, _factory.OpenedReadConnections);
        Assert.Equal(string.Equals(operation, "restore", StringComparison.Ordinal) ? 1 : 0, sourcePage.TotalCount);
        if (string.Equals(operation, "transfer", StringComparison.Ordinal))
        {
            Assert.Equal("destination.branch", Assert.Single(destinationPage.Results).Path);
        }
    }

    [Theory]
    [InlineData("same-values", 200)]
    [InlineData("empty-update", 200)]
    [InlineData("same-parent", 200)]
    [InlineData("invalid-update", 400)]
    [InlineData("cycle", 409)]
    [InlineData("active-children", 409)]
    [InlineData("already-active", 204)]
    public async Task FailedOrUnchangedOperation_ShouldPreserveCachedLists(string operation, int expectedStatus)
    {
        var root = await CreateAsync("Root", "root");
        var branch = await CreateAsync("Branch", "branch", root.Id);
        await ChildrenAsync(root.Id);
        await ChildrenAsync(branch.Id);

        using var response = operation switch
        {
            "same-values" => await _client.PatchAsJsonAsync($"departments/{branch.Id}",
                new UpdateDepartmentRequest("Branch", "branch")),
            "empty-update" => await _client.PatchAsJsonAsync($"departments/{branch.Id}",
                new UpdateDepartmentRequest(null, null)),
            "same-parent" => await _client.PutAsJsonAsync($"departments/{branch.Id}/parent",
                new TransferDepartmentRequest(root.Id)),
            "invalid-update" => await _client.PatchAsJsonAsync($"departments/{branch.Id}",
                new UpdateDepartmentRequest(null, "INVALID")),
            "cycle" => await _client.PutAsJsonAsync($"departments/{root.Id}/parent",
                new TransferDepartmentRequest(branch.Id)),
            "active-children" => await _client.DeleteAsync($"departments/{root.Id}"),
            _ => await _client.PostAsync($"departments/{branch.Id}/restore", null),
        };
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        var connections = _factory.OpenedReadConnections;

        Assert.Equal("Branch", Assert.Single((await ChildrenAsync(root.Id)).Results).Name);
        Assert.Empty((await ChildrenAsync(branch.Id)).Results);
        Assert.Equal(connections, _factory.OpenedReadConnections);
    }

    [Fact]
    public async Task RolledBackUpdate_ShouldPreserveCachedSubtree()
    {
        var root = await CreateAsync("Root", "root");
        var child = await CreateAsync("Child", "child", root.Id);
        await ChildrenAsync(root.Id);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync("""
            ALTER TABLE departments ADD CONSTRAINT reject_cached_descendant_path
            CHECK (path <> 'renamed.child'::ltree);
            """);
        try
        {
            var response = await _client.PatchAsJsonAsync($"departments/{root.Id}",
                new UpdateDepartmentRequest(null, "renamed"));
            Assert.Equal(500, (int)response.StatusCode);
            var connections = _factory.OpenedReadConnections;
            var node = Assert.Single((await ChildrenAsync(root.Id)).Results);
            Assert.Equal(child.Id, node.Id);
            Assert.Equal("root.child", node.Path);
            Assert.Equal(connections, _factory.OpenedReadConnections);
        }
        finally
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE departments DROP CONSTRAINT reject_cached_descendant_path;");
        }
    }

    private async Task<DepartmentDto> CreateAsync(string name, string slug, Guid? parentId = null)
    {
        var response = await _client.PostAsJsonAsync("departments",
            new CreateDepartmentRequest(name, slug, [], parentId));
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);
        return envelope.Result;
    }

    private async Task<PagedResult<DepartmentNodeDto>> ChildrenAsync(Guid id, int page = 1, int pageSize = 5)
    {
        var response = await _client.GetAsync($"departments/{id}/children?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<DepartmentNodeDto>>>();
        Assert.NotNull(envelope?.Result);
        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _factory.ResetDatabaseAsync();
}
