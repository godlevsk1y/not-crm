using System.Net.Http.Json;
using DirectoryService.Contracts.Departments;
using DirectoryService.Web.Results;

namespace DirectoryService.IntegrationTests.Departments.Commands;

public class CreateDepartmentTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly Func<Task> _resetDatabase;
    
    public CreateDepartmentTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
        _resetDatabase = factory.ResetDatabaseAsync;
    }

    [Fact]
    public async Task CreateDepartment_ShouldSucceed_WhenRequestIsValid()
    {
        var request = new CreateDepartmentRequest(
            Name: "Product Team",
            Slug: "product-team",
            LocationIds: [],
            ParentId: null
        );

        var response = await _client.PostAsJsonAsync("api/departments", request);
        response.EnsureSuccessStatusCode();
        
        Assert.Equal(201, (int)response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Result);
        Assert.False(envelope.IsError);
        Assert.Equal("Product Team", envelope.Result.Name);
        Assert.Equal("product-team", envelope.Result.Slug);
        Assert.NotEqual(Guid.Empty, envelope.Result.Id);
    }

    [Fact]
    public async Task CreateDepartment_ShouldReturnNotFound_WhenLocationDoesNotExist()
    {
        var notExistingLocationId = Guid.NewGuid();
        
        var request = new CreateDepartmentRequest(
            Name: "Product Team",
            Slug: "product-team",
            LocationIds: [notExistingLocationId],
            ParentId: null
        );
        
        var response = await _client.PostAsJsonAsync("api/departments", request);
        
        Assert.Equal(404, (int)response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());

        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("location.not.found", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task CreateDepartment_ShouldReturnBadRequest_WhenSlugIsInvalid()
    {
        var request = new CreateDepartmentRequest(
            Name: "Product Team",
            Slug: "123d\nINVALID-SLUG\n\t",
            LocationIds: [],
            ParentId: null
        );

        var response = await _client.PostAsJsonAsync("api/departments", request);
        
        Assert.Equal(400, (int)response.StatusCode);
        Assert.NotEmpty(await response.Content.ReadAsStringAsync());
        
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<DepartmentDto>>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("slug.invalid", envelope.Error!.Messages[0].Code);
    }
    
    
    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _resetDatabase();
}