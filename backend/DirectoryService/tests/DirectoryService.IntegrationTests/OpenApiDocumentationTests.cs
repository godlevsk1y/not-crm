using System.Net.Http.Json;
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
        Assert.Equal(24, count);
    }

    [Theory]
    [InlineData("/api/locations")]
    [InlineData("/api/positions")]
    public async Task OpenApi_ShouldDescribeQueryConstraintsAndRequestExamples(string path)
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var parameters = paths.GetProperty(path).GetProperty("get").GetProperty("parameters");
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
    [Fact]
    public async Task OpenApi_ShouldDocumentExactErrorCodesForEveryOperation()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");
        var apiDescription = document.RootElement.GetProperty("info").GetProperty("description").GetString()!;
        Assert.DoesNotContain("Error code catalogue", apiDescription, StringComparison.Ordinal);
        Assert.DoesNotContain("| code |", apiDescription, StringComparison.Ordinal);
        Assert.DoesNotContain("`internal.server.error`", apiDescription, StringComparison.Ordinal);
        Assert.Contains("Unmatched routes", apiDescription, StringComparison.Ordinal);

        const string departmentInput = "department.name.empty department.name.long slug.empty slug.invalid";
        const string locationInput = "location.name.empty location.name.long address.country.empty address.city.empty address.street.empty address.housenumber.empty";
        const string positionInput = "position.name.empty position.name.long";
        const string pages = "departments.page.invalid departments.page.size.invalid";
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["post /api/departments"] = departmentInput + " department.not.found location.not.found",
            ["patch /api/departments/{id}"] = departmentInput + " department.parent.itself department.not.found",
            ["delete /api/departments/{id}"] = "department.not.found department.has.active.children",
            ["get /api/departments/{id}"] = "department.not.found",
            ["put /api/departments/{id}/parent"] = "department.id.empty department.not.found department.parent.not.found department.transfer.parent_to_self department.transfer.cycle",
            ["post /api/departments/{departmentId}/locations/{locationId}"] = "department.not.found location.not.found department.location.already.added",
            ["post /api/departments/{departmentId}/positions/{positionId}"] = "department.not.found position.not.found department.position.already.added",
            ["delete /api/departments/{departmentId}/locations/{locationId}"] = "department.location.not.found",
            ["delete /api/departments/{departmentId}/positions/{positionId}"] = "department.position.not.found",
            ["get /api/departments"] = pages + " departments.search.too.long departments.sort.by.invalid departments.sort.direction.invalid",
            ["get /api/departments/tree"] = pages,
            ["get /api/departments/{id}/children"] = pages,
            ["get /api/departments/{id}/ancestors"] = pages,
            ["get /api/departments/tree/search"] = pages + " departments.search.query.invalid",
            ["post /api/locations"] = locationInput + " location.exists",
            ["patch /api/locations/{id}"] = locationInput + " location.exists location.not.found",
            ["delete /api/locations/{id}"] = "location.not.found",
            ["get /api/locations/{id}"] = "location.not.found",
            ["get /api/locations"] = "location.search.too.long location.department.count.invalid locations.sort.by.invalid locations.sort.direction.invalid locations.page.invalid locations.page.size.invalid",
            ["get /api/positions"] = "positions.search.too.long positions.sort.by.invalid positions.sort.direction.invalid positions.page.invalid positions.page.size.invalid",
            ["post /api/positions"] = positionInput,
            ["patch /api/positions/{id}"] = positionInput + " position.not.found",
            ["delete /api/positions/{id}"] = "position.not.found",
        };

        var allCodes = new HashSet<string>(StringComparer.Ordinal);
        var count = 0;
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                count++;
                var key = operation.Name + " " + path.Name;
                var operationDescription = operation.Value.GetProperty("description").GetString()!;
                Assert.Contains("### Error codes", operationDescription, StringComparison.Ordinal);
                Assert.Contains("server-side name", operationDescription, StringComparison.Ordinal);
                Assert.DoesNotContain("`department.locations.null`", operationDescription, StringComparison.Ordinal);
                Assert.DoesNotContain("#error-code-catalogue", operationDescription, StringComparison.Ordinal);
                var operationRows = operationDescription.Split('\n')
                    .Where(line => line.StartsWith("| `", StringComparison.Ordinal)).ToArray();
                var responseRows = new List<string>();
                // There are no business errors on the top-locations query.
                var codes = expected.TryGetValue(key, out var specific) ? specific : "";
                Assert.True(expected.ContainsKey(key) || string.Equals(key, "get /api/locations/top", StringComparison.Ordinal));
                codes += " internal.server.error";
                if (operation.Value.TryGetProperty("requestBody", out _) ||
                    operation.Value.TryGetProperty("parameters", out var parameters) &&
                    parameters.EnumerateArray().Any(p => string.Equals(p.GetProperty("in").GetString(), "query", StringComparison.Ordinal)))
                    codes += " invalid.model.state";
                if (!string.Equals(operation.Name, "get", StringComparison.Ordinal))
                    codes += " department.transfer.conflict";
                var expectedCodes = codes.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal);
                var actualCodes = new HashSet<string>(StringComparer.Ordinal);
                foreach (var result in operation.Value.GetProperty("responses").EnumerateObject())
                {
                    if (result.Name is not ("400" or "404" or "409" or "500"))
                        continue;
                    var description = result.Value.GetProperty("description").GetString()!;
                    Assert.DoesNotContain("#error-code-catalogue", description, StringComparison.Ordinal);
                    foreach (var row in description.Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal)))
                    {
                        var cells = row.Split('|');
                        var code = cells[1].Trim().Trim('`');
                        Assert.Equal(result.Name, cells[2].Trim());
                        var expectedType = code switch
                        {
                            "invalid.model.state" => "BadRequest",
                            "internal.server.error" => "Internal",
                            "department.parent.itself" => "Domain",
                            _ when code.StartsWith("address.", StringComparison.Ordinal) &&
                                string.Equals(operation.Name, "patch", StringComparison.Ordinal) => "Domain",
                            _ when code.EndsWith(".not.found", StringComparison.Ordinal) => "NotFound",
                            "location.exists" or "department.has.active.children" or
                                "department.location.already.added" or "department.position.already.added" => "Conflict",
                            _ when code.StartsWith("department.transfer.", StringComparison.Ordinal) => "Conflict",
                            _ => "Validation",
                        };
                        Assert.Equal(expectedType, cells[3].Trim());
                        Assert.Equal(expectedType switch
                        {
                            "NotFound" => "404",
                            "Conflict" => "409",
                            "Internal" => "500",
                            _ => "400",
                        }, result.Name);
                        Assert.False(string.IsNullOrWhiteSpace(cells[4]));
                        var expectedField = code switch
                        {
                            "department.name.empty" or "department.name.long" => "DepartmentName",
                            "location.name.empty" or "location.name.long" => "LocationName",
                            "position.name.empty" or "position.name.long" => "PositionName",
                            "address.country.empty" => "Country",
                            "address.city.empty" => "City",
                            "address.street.empty" => "Street",
                            "address.housenumber.empty" => "HouseNumber",
                            "department.id.empty" => "DepartmentId",
                            "location.department.count.invalid" => "MinDepartmentCount",
                            _ when code.Contains(".page.size.", StringComparison.Ordinal) => "PageSize",
                            _ when code.Contains(".page.", StringComparison.Ordinal) => "Page",
                            _ when code.Contains(".sort.by.", StringComparison.Ordinal) => "SortBy",
                            _ when code.Contains(".sort.direction.", StringComparison.Ordinal) => "SortDirection",
                            _ when code.Contains(".search.", StringComparison.Ordinal) => "Search",
                            _ => "null",
                        };
                        Assert.Equal($"`{expectedField}`", cells[5].Trim());
                        Assert.False(string.IsNullOrWhiteSpace(cells[6]));
                        Assert.Contains(row, operationRows);
                        responseRows.Add(row);
                        actualCodes.Add(code);
                    }
                }
                var operationCodes = operationRows.Select(row => row.Split('|')[1].Trim().Trim('`'));
                Assert.Equal(expectedCodes.Order(StringComparer.Ordinal), operationCodes.Order(StringComparer.Ordinal));
                Assert.Equal(responseRows.Order(StringComparer.Ordinal), operationRows.Order(StringComparer.Ordinal));
                Assert.Equal(expectedCodes.Order(StringComparer.Ordinal), actualCodes.Order(StringComparer.Ordinal));
                allCodes.UnionWith(actualCodes);
            }
        }
        Assert.Equal(24, count);
        Assert.Equal(46, allCodes.Count);
    }

    [Theory]
    [InlineData("/api/locations", "post", "400", "Validation", "Validation")]
    [InlineData("/api/locations/{id}", "patch", "400", "Domain", "Domain")]
    [InlineData("/api/locations/{id}", "get", "404", "NotFound", "NotFound")]
    [InlineData("/api/departments/{id}/parent", "put", "409", "Conflict", "Conflict")]
    [InlineData("/api/positions", "post", "400", "BadRequest", "BadRequest")]
    [InlineData("/api/positions", "post", "500", "Internal", "Internal")]
    public async Task OpenApi_ExamplesShouldMatchSerializedResponses(
        string path, string method, string status, string exampleName, string errorType)
    {
        using var openApi = await _client.GetAsync("/openapi/v1.json");
        openApi.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await openApi.Content.ReadAsStringAsync());
        var example = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method)
            .GetProperty("responses").GetProperty(status).GetProperty("content").GetProperty("application/json")
            .GetProperty("examples").GetProperty(exampleName).GetProperty("value");
        Assert.Equal(JsonValueKind.Null, example.GetProperty("result").ValueKind);
        Assert.True(example.GetProperty("isError").GetBoolean());
        Assert.True(example.GetProperty("timeGenerated").TryGetDateTimeOffset(out _));
        Assert.Equal(errorType, example.GetProperty("error").GetProperty("type").GetString());
        foreach (var message in example.GetProperty("error").GetProperty("messages").EnumerateArray())
        {
            Assert.False(string.IsNullOrWhiteSpace(message.GetProperty("code").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(message.GetProperty("message").GetString()));
            Assert.Contains(message.GetProperty("invalidField").ValueKind, new[] { JsonValueKind.Null, JsonValueKind.String });
        }

        // These failures run through the real handlers, MVC validation and exception middleware.
        var resourceId = Guid.Empty;
        if (exampleName is "Domain" or "Conflict")
        {
            var resourcePath = exampleName is "Domain" ? "/api/locations" : "/api/departments";
            using var created = await _client.PostAsJsonAsync(resourcePath, new
            {
                name = Guid.NewGuid().ToString(), slug = "example", locationIds = Array.Empty<Guid>(),
                country = "Russia", city = "Moscow", street = "Tverskaya", houseNumber = "1",
            });
            created.EnsureSuccessStatusCode();
            using var createdBody = JsonDocument.Parse(await created.Content.ReadAsStringAsync());
            resourceId = createdBody.RootElement.GetProperty("result").GetProperty("id").GetGuid();
        }
        using var malformedJson = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var actualResponse = exampleName switch
        {
            "Validation" => await _client.PostAsJsonAsync("/api/locations", new
            {
                name = " ", country = " ", city = "Moscow", street = "Tverskaya", houseNumber = "1",
            }),
            "NotFound" => await _client.GetAsync("/api/locations/11111111-1111-1111-1111-111111111111"),
            "BadRequest" => await _client.PostAsync("/api/positions", malformedJson),
            "Internal" => await _client.PostAsJsonAsync("/api/locations", new
            {
                name = Guid.NewGuid().ToString(), country = new string('x', 61), city = "Moscow", street = "Tverskaya", houseNumber = "1",
            }),
            "Domain" => await _client.PatchAsJsonAsync($"/api/locations/{resourceId}", new { country = " " }),
            "Conflict" => await _client.PutAsJsonAsync($"/api/departments/{resourceId}/parent", new { newParentId = resourceId }),
            _ => throw new InvalidOperationException("Unknown example"),
        };
        Assert.Equal(int.Parse(status, System.Globalization.CultureInfo.InvariantCulture), (int)actualResponse.StatusCode);
        using var actual = JsonDocument.Parse(await actualResponse.Content.ReadAsStringAsync());
        Assert.True(JsonElement.DeepEquals(example.GetProperty("error"), actual.RootElement.GetProperty("error")));
    }
    [Theory]
    [InlineData("{\"name\":\"Team\",\"slug\":\"team\",\"locationIds\":null}")]
    [InlineData("{\"name\":\"Team\",\"slug\":\"team\"}")]
    public async Task OpenApi_ShouldExcludeValidatorErrorsInterceptedByModelBinding(string json)
    {
        using var body = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _client.PostAsync("/api/departments", body);
        Assert.Equal(400, (int)response.StatusCode);
        using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid.model.state", result.RootElement.GetProperty("error").GetProperty("messages")[0].GetProperty("code").GetString());
    }
}
