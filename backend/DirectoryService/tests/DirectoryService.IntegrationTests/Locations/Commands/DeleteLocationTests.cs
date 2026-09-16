using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Ids;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Web.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests.Locations.Commands;

public class DeleteLocationTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly Func<Task> _resetDatabase;

    public DeleteLocationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _factory = factory;
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task DeleteLocation_ShouldSucceed_WhenLocationExists()
    {
        var location = await CreateLocationAsync();

        var response = await _client.DeleteAsync($"api/locations/{location.Id}");

        Assert.Equal(204, (int)response.StatusCode);

        var getResponse = await _client.GetAsync($"api/locations/{location.Id}");
        Assert.Equal(404, (int)getResponse.StatusCode);

        var envelope = await getResponse.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.not.found", envelope.Error!.Messages[0].Code);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();

        var deletedLocation = await dbContext.Locations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(l => l.Id == new LocationId(location.Id));

        Assert.NotNull(deletedLocation);
        Assert.NotNull(deletedLocation.DeletedAt);
    }

    [Fact]
    public async Task DeleteLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        var locationId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"api/locations/{locationId}");

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task DeleteLocation_ShouldRemoveDepartmentRelations()
    {
        var departmentId = await CreateDepartmentAsync();
        var location = await CreateLocationAsync();
        var addResponse = await _client.PostAsync(
            $"api/departments/{departmentId}/locations/{location.Id}",
            content: null);
        addResponse.EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync($"api/locations/{location.Id}");

        Assert.Equal(204, (int)response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();

        var relationExists = await dbContext.DepartmentLocations.AnyAsync(
            relation =>
                relation.DepartmentId == new DepartmentId(departmentId) &&
                relation.LocationId == new LocationId(location.Id));

        Assert.False(relationExists);
    }

    [Fact]
    public async Task DeleteLocation_ShouldRollbackRelationRemoval_WhenSoftDeleteSaveFails()
    {
        var departmentId = await CreateDepartmentAsync();
        var location = await CreateLocationAsync();
        var addResponse = await _client.PostAsync(
            $"api/departments/{departmentId}/locations/{location.Id}",
            content: null);
        addResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            await dbContext.Database.ExecuteSqlRawAsync("""
                ALTER TABLE locations
                ADD CONSTRAINT ck_locations_reject_soft_delete
                CHECK (deleted_at IS NULL)
                """);

            try
            {
                response = await _client.DeleteAsync($"api/locations/{location.Id}");
            }
            finally
            {
                await dbContext.Database.ExecuteSqlRawAsync("""
                    ALTER TABLE locations
                    DROP CONSTRAINT IF EXISTS ck_locations_reject_soft_delete
                    """);
            }
        }

        Assert.Equal(500, (int)response.StatusCode);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationContext = verificationScope.ServiceProvider
            .GetRequiredService<DirectoryServiceDbContext>();

        var persistedLocation = await verificationContext.Locations
            .IgnoreQueryFilters()
            .SingleAsync(entity => entity.Id == new LocationId(location.Id));
        var relationExists = await verificationContext.DepartmentLocations.AnyAsync(
            relation =>
                relation.DepartmentId == new DepartmentId(departmentId) &&
                relation.LocationId == new LocationId(location.Id));

        Assert.Null(persistedLocation.DeletedAt);
        Assert.True(relationExists);
    }

    private async Task<Guid> CreateDepartmentAsync()
    {
        var request = new CreateDepartmentRequest(
            Name: "Product Team",
            Slug: "product-team",
            LocationIds: [],
            ParentId: null);

        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope?.Result);

        return envelope.Result.Id;
    }

    private async Task<LocationDto> CreateLocationAsync()
    {
        var request = new CreateLocationRequest(
            Name: "Moscow Office",
            Country: "Russia",
            Region: "Moscow",
            City: "Moscow",
            District: null,
            Street: "Tverskaya Street",
            HouseNumber: "1",
            PostalCode: "125009");

        var response = await _client.PostAsJsonAsync("api/locations", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<LocationDto>>();
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Result);

        return envelope.Result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
