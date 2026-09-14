using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class DepartmentLocationTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public DepartmentLocationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task AddLocation_ShouldSucceed_WhenDepartmentAndLocationExist()
    {
        var departmentId = await CreateDepartmentAsync();
        var locationId = await CreateLocationAsync();

        var response = await _client.PostAsync(
            $"api/departments/{departmentId}/locations/{locationId}",
            content: null);

        Assert.Equal(204, (int)response.StatusCode);

        var removeResponse = await _client.DeleteAsync(
            $"api/departments/{departmentId}/locations/{locationId}");

        Assert.Equal(204, (int)removeResponse.StatusCode);
    }

    [Fact]
    public async Task AddLocation_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var locationId = await CreateLocationAsync();

        var response = await _client.PostAsync(
            $"api/departments/{Guid.NewGuid()}/locations/{locationId}",
            content: null);

        Assert.Equal(404, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "department.not.found");
    }

    [Fact]
    public async Task AddLocation_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        var departmentId = await CreateDepartmentAsync();

        var response = await _client.PostAsync(
            $"api/departments/{departmentId}/locations/{Guid.NewGuid()}",
            content: null);

        Assert.Equal(404, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "location.not.found");
    }

    [Fact]
    public async Task AddLocation_ShouldReturnConflict_WhenLocationIsAlreadyAdded()
    {
        var departmentId = await CreateDepartmentAsync();
        var locationId = await CreateLocationAsync();
        var route = $"api/departments/{departmentId}/locations/{locationId}";

        var firstResponse = await _client.PostAsync(route, content: null);
        firstResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsync(route, content: null);

        Assert.Equal(409, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "department.location.already.added");
    }

    [Fact]
    public async Task RemoveLocation_ShouldSucceed_WhenLocationIsAdded()
    {
        var departmentId = await CreateDepartmentAsync();
        var locationId = await CreateLocationAsync();
        var route = $"api/departments/{departmentId}/locations/{locationId}";
        var addResponse = await _client.PostAsync(route, content: null);
        addResponse.EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync(route);

        Assert.Equal(204, (int)response.StatusCode);

        var addAgainResponse = await _client.PostAsync(route, content: null);
        Assert.Equal(204, (int)addAgainResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveLocation_ShouldReturnNotFound_WhenLocationIsNotAdded()
    {
        var departmentId = await CreateDepartmentAsync();
        var locationId = await CreateLocationAsync();

        var response = await _client.DeleteAsync(
            $"api/departments/{departmentId}/locations/{locationId}");

        Assert.Equal(404, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "department.location.not.found");
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

    private async Task<Guid> CreateLocationAsync()
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
        Assert.NotNull(envelope?.Result);

        return envelope.Result.Id;
    }

    private static async Task AssertErrorCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();

        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal(expectedCode, envelope.Error!.Messages[0].Code);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}
