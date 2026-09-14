using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class DeleteDepartmentTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public DeleteDepartmentTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task DeleteDepartment_ShouldSucceed_WhenDepartmentExists()
    {
        var department = await CreateDepartmentAsync();

        var response = await _client.DeleteAsync($"api/departments/{department.Id}");

        Assert.Equal(204, (int)response.StatusCode);

        var getResponse = await _client.GetAsync($"api/departments/{department.Id}");
        Assert.Equal(404, (int)getResponse.StatusCode);

        var envelope = await getResponse.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("department.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task DeleteDepartment_ShouldReturnNotFound_WhenDepartmentDoesNotExist()
    {
        var departmentId = Guid.NewGuid();

        var response = await _client.DeleteAsync($"api/departments/{departmentId}");

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
