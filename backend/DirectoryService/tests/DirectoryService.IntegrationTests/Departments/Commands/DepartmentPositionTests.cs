using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Positions;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class DepartmentPositionTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public DepartmentPositionTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task AddPosition_ShouldSucceed_WhenDepartmentAndPositionExist()
    {
        var departmentId = await CreateDepartmentAsync();
        var positionId = await CreatePositionAsync();

        var response = await _client.PostAsync(
            $"api/departments/{departmentId}/positions/{positionId}",
            content: null);

        Assert.Equal(204, (int)response.StatusCode);

        var removeResponse = await _client.DeleteAsync(
            $"api/departments/{departmentId}/positions/{positionId}");

        Assert.Equal(204, (int)removeResponse.StatusCode);
    }

    [Fact]
    public async Task AddPosition_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var positionId = await CreatePositionAsync();

        var response = await _client.PostAsync(
            $"api/departments/{Guid.NewGuid()}/positions/{positionId}",
            content: null);

        Assert.Equal(404, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "department.not.found");
    }

    [Fact]
    public async Task AddPosition_ShouldReturnNotFound_WhenPositionDoesNotExist()
    {
        var departmentId = await CreateDepartmentAsync();

        var response = await _client.PostAsync(
            $"api/departments/{departmentId}/positions/{Guid.NewGuid()}",
            content: null);

        Assert.Equal(404, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "position.not.found");
    }

    [Fact]
    public async Task AddPosition_ShouldReturnConflict_WhenPositionIsAlreadyAdded()
    {
        var departmentId = await CreateDepartmentAsync();
        var positionId = await CreatePositionAsync();
        var route = $"api/departments/{departmentId}/positions/{positionId}";

        var firstResponse = await _client.PostAsync(route, content: null);
        firstResponse.EnsureSuccessStatusCode();

        var response = await _client.PostAsync(route, content: null);

        Assert.Equal(409, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "department.position.already.added");
    }

    [Fact]
    public async Task RemovePosition_ShouldSucceed_WhenPositionIsAdded()
    {
        var departmentId = await CreateDepartmentAsync();
        var positionId = await CreatePositionAsync();
        var route = $"api/departments/{departmentId}/positions/{positionId}";
        var addResponse = await _client.PostAsync(route, content: null);
        addResponse.EnsureSuccessStatusCode();

        var response = await _client.DeleteAsync(route);

        Assert.Equal(204, (int)response.StatusCode);

        var addAgainResponse = await _client.PostAsync(route, content: null);
        Assert.Equal(204, (int)addAgainResponse.StatusCode);
    }

    [Fact]
    public async Task RemovePosition_ShouldReturnNotFound_WhenPositionIsNotAdded()
    {
        var departmentId = await CreateDepartmentAsync();
        var positionId = await CreatePositionAsync();

        var response = await _client.DeleteAsync(
            $"api/departments/{departmentId}/positions/{positionId}");

        Assert.Equal(404, (int)response.StatusCode);
        await AssertErrorCodeAsync(response, "department.position.not.found");
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

    private async Task<Guid> CreatePositionAsync()
    {
        var request = new CreatePositionRequest("Software Engineer");

        var response = await _client.PostAsJsonAsync("api/positions", request);
        response.EnsureSuccessStatusCode();

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PositionDto>>();
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
