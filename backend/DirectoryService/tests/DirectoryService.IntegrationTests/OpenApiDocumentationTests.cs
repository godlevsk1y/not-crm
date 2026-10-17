using System.Text.Json;

namespace DirectoryService.IntegrationTests;

public class OpenApiDocumentationTests : IClassFixture<DirectoryServiceTestWebFactory>
{
    private readonly HttpClient _client;

    public OpenApiDocumentationTests(DirectoryServiceTestWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OpenApi_ShouldDescribeEveryOperationAndResponseEnvelope()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var count = 0;
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                count++;
                Assert.False(string.IsNullOrWhiteSpace(operation.Value.GetProperty("summary").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(operation.Value.GetProperty("description").GetString()));
                var responses = operation.Value.GetProperty("responses");
                Assert.True(responses.TryGetProperty("500", out _));
                foreach (var result in responses.EnumerateObject())
                {
                    if (string.Equals(result.Name, "204", StringComparison.Ordinal))
                    {
                        Assert.False(result.Value.TryGetProperty("content", out _));
                        continue;
                    }

                    var schema = result.Value.GetProperty("content").GetProperty("application/json").GetProperty("schema");
                    var reference = schema.GetProperty("$ref").GetString();
                    Assert.Contains("Envelope", reference, StringComparison.Ordinal);
                }
            }
        }
        Assert.Equal(23, count);
    }

    [Fact]
    public async Task OpenApi_ShouldDescribeQueryConstraintsAndRequestExamples()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var parameters = paths.GetProperty("/api/locations").GetProperty("get").GetProperty("parameters");
        var pageSize = parameters.EnumerateArray().Single(p =>
            string.Equals(p.GetProperty("name").GetString(), "PageSize", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(5, pageSize.GetProperty("schema").GetProperty("minimum").GetInt32());
        Assert.Equal(100, pageSize.GetProperty("schema").GetProperty("maximum").GetInt32());
        Assert.Equal(20, pageSize.GetProperty("schema").GetProperty("default").GetInt32());

        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        var create = schemas.GetProperty("CreateDepartmentRequest");
        Assert.Contains(create.GetProperty("required").EnumerateArray(), p =>
            string.Equals(p.GetString(), "locationIds", StringComparison.Ordinal));
        Assert.Equal("engineering", create.GetProperty("example").GetProperty("slug").GetString());
        var update = schemas.GetProperty("UpdateDepartmentRequest");
        Assert.False(update.TryGetProperty("required", out var required) && required.GetArrayLength() > 0);
        Assert.Contains("Empty UUID", update.GetProperty("properties").GetProperty("parentId").GetProperty("description").GetString(), StringComparison.Ordinal);
    }
}
