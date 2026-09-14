using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetDepartmentByIdTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public GetDepartmentByIdTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task GetDepartmentById_ShouldReturnDepartment_WhenDepartmentExists()
    {
        var createdDepartment = await CreateDepartmentAsync();

        var response = await _client.GetAsync($"api/departments/{createdDepartment.Id}");

        Assert.Equal(200, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        Assert.Equal(createdDepartment.Id, envelope.Result.Id);
        Assert.Equal("Product Team", envelope.Result.Name);
        Assert.Equal("product-team", envelope.Result.Slug);
        Assert.Equal("product-team", envelope.Result.Path);
        Assert.Null(envelope.Result.ParentId);
    }

    [Fact]
    public async Task GetDepartmentById_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var departmentId = Guid.NewGuid();

        var response = await _client.GetAsync($"api/departments/{departmentId}");

        Assert.Equal(404, (int)response.StatusCode);

        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("department.not.found", envelope.Error!.Messages[0].Code);
    }

    private async Task<DepartmentDto> CreateDepartmentAsync()
    {
        var request = new CreateDepartmentRequest(
            Name: "Product Team",
            Slug: "product-team",
            LocationIds: [],
            ParentId: null
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
