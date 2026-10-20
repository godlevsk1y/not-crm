using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace DirectoryService.IntegrationTests;

public class OpenApiDocumentationTests : IClassFixture<DirectoryServiceTestWebFactory>
{
    private readonly HttpClient _client;

    public OpenApiDocumentationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development")).CreateClient();
    }

    [Fact]
    public async Task OpenApi_ShouldUseCamelCase_ForAllQueryParameters()
    {
        using var document = await _client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
        Assert.NotNull(document);

        var queryNames = new List<string>();
        foreach (var path in document.RootElement.GetProperty("paths").EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                if (!operation.Value.TryGetProperty("parameters", out var parameters))
                    continue;

                foreach (var parameter in parameters.EnumerateArray())
                {
                    if (string.Equals(parameter.GetProperty("in").GetString(), "query", StringComparison.Ordinal))
                        queryNames.Add(parameter.GetProperty("name").GetString()!);
                }
            }
        }

        Assert.NotEmpty(queryNames);
        Assert.All(queryNames, name => Assert.Equal(JsonNamingPolicy.CamelCase.ConvertName(name), name));
    }

    [Theory]
    [InlineData("/api/locations", new[] { "search", "minDepartmentCount", "sortBy", "sortDirection", "page", "pageSize" })]
    [InlineData("/api/departments", new[] { "search", "sortBy", "sortDirection", "page", "pageSize" })]
    [InlineData("/api/positions", new[] { "search", "sortBy", "sortDirection", "page", "pageSize" })]
    [InlineData("/api/departments/tree", new[] { "page", "pageSize" })]
    [InlineData("/api/departments/tree/search", new[] { "q", "page", "pageSize" })]
    [InlineData("/api/departments/{id}/children", new[] { "id", "page", "pageSize" })]
    [InlineData("/api/departments/{id}/ancestors", new[] { "id", "page", "pageSize" })]
    public async Task OpenApi_ShouldDocumentExpectedParameterNames(string route, string[] expectedNames)
    {
        using var document = await _client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");
        Assert.NotNull(document);

        var path = document.RootElement.GetProperty("paths").EnumerateObject()
            .Single(path => string.Equals(path.Name, route, StringComparison.OrdinalIgnoreCase));
        var names = path.Value.GetProperty("get").GetProperty("parameters").EnumerateArray()
            .Select(parameter => parameter.GetProperty("name").GetString()).ToArray();

        Assert.Equal(expectedNames.Order(StringComparer.Ordinal), names.Order(StringComparer.Ordinal));
    }
}
