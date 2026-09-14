using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class UpdateDepartmentTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;

    public UpdateDepartmentTests(DirectoryServiceTestWebFactory factory)
    {
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
        Assert.Equal("engineering/platform-team", getEnvelope.Result.Path);
        Assert.Equal(parent.Id, getEnvelope.Result.ParentId);
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

    private async Task<DepartmentDto> CreateDepartmentAsync(string name, string slug)
    {
        var request = new CreateDepartmentRequest(
            Name: name,
            Slug: slug,
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
