using System.Net.Http.Json;
using System.Text.Json;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shared.Framework.Results;

namespace DirectoryService.IntegrationTests.Restore;

public class RestoreRecordsTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public RestoreRecordsTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("departments")]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task Restore_ShouldPreserveDataAndMoveRecordToActiveList(string resource)
    {
        var id = await SeedAsync(resource, archived: true);
        var before = await ReadAsync(resource, id);
        Assert.Contains(id, await ListIdsAsync(resource, isActive: false));
        Assert.DoesNotContain(id, await ListIdsAsync(resource, isActive: true));

        var response = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        var after = await ReadAsync(resource, id);
        Assert.Null(after.DeletedAt);
        Assert.True(after.UpdatedAt > before.UpdatedAt);
        Assert.Equal(before.Data, after.Data);
        Assert.Contains(id, await ListIdsAsync(resource, isActive: true));
        Assert.DoesNotContain(id, await ListIdsAsync(resource, isActive: false));

        var repeated = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);

        Assert.Equal(204, (int)repeated.StatusCode);
        Assert.Equal(after, await ReadAsync(resource, id));
    }

    [Theory]
    [InlineData("departments")]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task Restore_ShouldLeaveOriginallyActiveRecordUnchanged(string resource)
    {
        var id = await SeedAsync(resource, archived: false);
        var before = await ReadAsync(resource, id);

        var response = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Equal(before, await ReadAsync(resource, id));
    }

    [Theory]
    [InlineData("departments", "department.not.found", false)]
    [InlineData("locations", "location.not.found", false)]
    [InlineData("positions", "position.not.found", false)]
    [InlineData("departments", "department.not.found", true)]
    [InlineData("locations", "location.not.found", true)]
    [InlineData("positions", "position.not.found", true)]
    public async Task Restore_ShouldReturnNotFound_WhenRecordDoesNotExist(
        string resource, string code, bool physicallyDeleted)
    {
        var id = Guid.NewGuid();
        if (physicallyDeleted)
        {
            id = await SeedAsync(resource, archived: true);
            await using var scope = _factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            context.Remove(await FindAsync(context, resource, id));
            await context.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);

        await AssertErrorAsync(response, 404, code);
    }

    [Theory]
    [InlineData("departments")]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task Restore_ShouldProtectOldRecordFromRetentionCleanup(string resource)
    {
        var id = await SeedAsync(resource, archived: true);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var entity = await FindAsync(context, resource, id);
        context.Entry(entity).Property("DeletedAt").CurrentValue = DateTime.UtcNow.AddDays(-31);
        await context.SaveChangesAsync();

        var response = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);
        Assert.Equal(204, (int)response.StatusCode);
        var cleanup = new DatabaseCleanupService(context, Options.Create(new DatabaseCleanupOptions()));
        Assert.Equal(0, await cleanup.CleanupAsync(CancellationToken.None));

        Assert.Null((await ReadAsync(resource, id)).DeletedAt);
    }

    [Fact]
    public async Task RestoreDepartment_ShouldRequireActiveParentAndPreserveHierarchyAndLinks()
    {
        var parent = Department.Create(DepartmentName.Create("Parent Team").Value, Slug.Create("parent").Value);
        var department = Department.Create(DepartmentName.Create("Product Team").Value, Slug.Create("product").Value, parent);
        var child = Department.Create(DepartmentName.Create("Child Team").Value, Slug.Create("child").Value, department);
        var location = CreateLocation();
        var position = Position.Create(PositionName.Create("Developer").Value);
        var locationLink = new DepartmentLocation(department.Id, location.Id, isPrimary: true);
        var positionLink = new DepartmentPosition(department.Id, position.Id);
        parent.SoftDelete();
        department.SoftDelete();
        child.SoftDelete();
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            context.AddRange(parent, department, child, location, position, locationLink, positionLink);
            await context.SaveChangesAsync();
        }
        var before = await ReadAsync("departments", department.Id.Value);

        var conflict = await _client.PostAsync($"api/departments/{department.Id.Value}/restore", content: null);

        await AssertErrorAsync(conflict, 409, "department.parent.archived");
        Assert.Equal(before, await ReadAsync("departments", department.Id.Value));
        var parentResponse = await _client.PostAsync($"api/departments/{parent.Id.Value}/restore", content: null);
        Assert.Equal(204, (int)parentResponse.StatusCode);
        Assert.NotNull((await ReadAsync("departments", department.Id.Value)).DeletedAt);

        var response = await _client.PostAsync($"api/departments/{department.Id.Value}/restore", content: null);

        Assert.Equal(204, (int)response.StatusCode);
        Assert.Equal(before.Data, (await ReadAsync("departments", department.Id.Value)).Data);
        Assert.NotNull((await ReadAsync("departments", child.Id.Value)).DeletedAt);
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var savedLocationLink = await verification.DepartmentLocations.SingleAsync();
        Assert.Equal(locationLink.Id, savedLocationLink.Id);
        Assert.True(savedLocationLink.IsPrimary);
        Assert.Equal(positionLink.Id, (await verification.DepartmentPositions.SingleAsync()).Id);
    }

    [Theory]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task Restore_ShouldNotRecreateDeletedRelationships(string resource)
    {
        var departmentId = await SeedAsync("departments", archived: false);
        var id = await SeedAsync(resource, archived: false);
        var link = await _client.PostAsync($"api/departments/{departmentId}/{resource}/{id}", content: null);
        link.EnsureSuccessStatusCode();
        var delete = await _client.DeleteAsync($"api/{resource}/{id}");
        Assert.Equal(204, (int)delete.StatusCode);

        var response = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);

        Assert.Equal(204, (int)response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        Assert.False(await context.DepartmentLocations.AnyAsync());
        Assert.False(await context.DepartmentPositions.AnyAsync());
    }

    [Theory]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task Restore_ShouldPreserveSurvivingRelationships(string resource)
    {
        var departmentId = await SeedAsync("departments", archived: false);
        var id = await SeedAsync(resource, archived: true);
        var linkId = Guid.Empty;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            if (string.Equals(resource, "locations", StringComparison.Ordinal))
            {
                var link = new DepartmentLocation(new DepartmentId(departmentId), new LocationId(id), isPrimary: true);
                linkId = link.Id;
                context.Add(link);
            }
            else
            {
                var link = new DepartmentPosition(new DepartmentId(departmentId), new PositionId(id));
                linkId = link.Id;
                context.Add(link);
            }
            await context.SaveChangesAsync();
        }

        var response = await _client.PostAsync($"api/{resource}/{id}/restore", content: null);

        Assert.Equal(204, (int)response.StatusCode);
        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verification = verificationScope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        if (string.Equals(resource, "locations", StringComparison.Ordinal))
        {
            var link = await verification.DepartmentLocations.SingleAsync();
            Assert.Equal(linkId, link.Id);
            Assert.True(link.IsPrimary);
        }
        else
        {
            Assert.Equal(linkId, (await verification.DepartmentPositions.SingleAsync()).Id);
        }
    }

    [Fact]
    public async Task RestoreLocation_ShouldReturnConflictWithoutChangingData_WhenSaveViolatesUniqueConstraint()
    {
        var id = await SeedAsync("locations", archived: true);
        var before = await ReadAsync("locations", id);
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        // The current unique index covers archived names too, so a duplicate cannot be seeded.
        // Inject a database uniqueness failure on restoration to exercise the HTTP/save failure path.
        await context.Database.ExecuteSqlRawAsync("""
            CREATE FUNCTION reject_location_restore() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                IF OLD.deleted_at IS NOT NULL AND NEW.deleted_at IS NULL THEN
                    RAISE EXCEPTION 'Location name conflict'
                        USING ERRCODE = '23505', CONSTRAINT = 'uq_location_name';
                END IF;
                RETURN NEW;
            END;
            $$;
            CREATE TRIGGER reject_location_restore BEFORE UPDATE ON locations
                FOR EACH ROW EXECUTE FUNCTION reject_location_restore();
            """);
        try
        {
            var response = await _client.PostAsync($"api/locations/{id}/restore", content: null);

            await AssertErrorAsync(response, 409, "location.exists");
            Assert.Equal(before, await ReadAsync("locations", id));
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER reject_location_restore ON locations;
                DROP FUNCTION reject_location_restore();
                """);
        }
    }

    [Fact]
    public async Task Restore_ShouldBeDocumentedInOpenApi()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("openapi/v1.json"));
        foreach (var resource in new[] { "departments", "locations", "positions" })
        {
            var operation = document.RootElement.GetProperty("paths").GetProperty($"/api/{resource}/{{id}}/restore").GetProperty("post");
            Assert.False(operation.TryGetProperty("requestBody", out _));
            var responses = operation.GetProperty("responses");
            Assert.True(responses.TryGetProperty("204", out _));
            Assert.True(responses.TryGetProperty("404", out _));
            Assert.True(responses.TryGetProperty("409", out _));
        }
        var health = await _client.GetAsync("health/live");
        Assert.Equal(200, (int)health.StatusCode);
    }

    private async Task<Guid> SeedAsync(string resource, bool archived)
    {
        object entity = resource switch
        {
            "departments" => Department.Create(DepartmentName.Create("Product Team").Value, Slug.Create("product-team").Value),
            "locations" => CreateLocation(),
            "positions" => Position.Create(PositionName.Create("Developer").Value),
            _ => throw new ArgumentOutOfRangeException(nameof(resource)),
        };
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        context.Add(entity);
        context.Entry(entity).Property("CreatedAt").CurrentValue = DateTime.UtcNow.AddDays(-10);
        context.Entry(entity).Property("UpdatedAt").CurrentValue = DateTime.UtcNow.AddDays(-5);
        if (archived)
            context.Entry(entity).Property("DeletedAt").CurrentValue = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();
        return entity switch
        {
            Department d => d.Id.Value,
            Location l => l.Id.Value,
            Position p => p.Id.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(resource)),
        };
    }

    private async Task<StoredRecord> ReadAsync(string resource, Guid id)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        return await FindAsync(context, resource, id) switch
        {
            Department d => new StoredRecord(d.UpdatedAt, d.DeletedAt,
                JsonSerializer.Serialize(new { d.Id, d.Name, d.Slug, d.Path, d.ParentId, d.Depth, d.CreatedAt })),
            Location l => new StoredRecord(l.UpdatedAt, l.DeletedAt,
                JsonSerializer.Serialize(new { l.Id, l.Name, l.Address, l.CreatedAt })),
            Position p => new StoredRecord(p.UpdatedAt, p.DeletedAt,
                JsonSerializer.Serialize(new { p.Id, p.Name, p.CreatedAt })),
            _ => throw new ArgumentOutOfRangeException(nameof(resource)),
        };
    }

    private static async Task<object> FindAsync(DirectoryServiceDbContext context, string resource, Guid id) => resource switch
    {
        "departments" => await context.Departments.IgnoreQueryFilters().SingleAsync(d => d.Id == new DepartmentId(id)),
        "locations" => await context.Locations.IgnoreQueryFilters().SingleAsync(l => l.Id == new LocationId(id)),
        "positions" => await context.Positions.IgnoreQueryFilters().SingleAsync(p => p.Id == new PositionId(id)),
        _ => throw new ArgumentOutOfRangeException(nameof(resource)),
    };

    private async Task<Guid[]> ListIdsAsync(string resource, bool isActive)
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync($"api/{resource}?isActive={isActive}"));
        return document.RootElement.GetProperty("result").GetProperty("results").EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid()).ToArray();
    }

    private static Location CreateLocation() => Location.Create(
        LocationName.Create("Moscow Office").Value,
        Address.Create("Russia", "Moscow", "Moscow", null, "Tverskaya", "1", "125009").Value);

    private static async Task AssertErrorAsync(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope?.Error);
        Assert.Equal(code, Assert.Single(envelope.Error.Messages).Code);
    }

    private sealed record StoredRecord(DateTime UpdatedAt, DateTime? DeletedAt, string Data);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => _factory.ResetDatabaseAsync();
}
