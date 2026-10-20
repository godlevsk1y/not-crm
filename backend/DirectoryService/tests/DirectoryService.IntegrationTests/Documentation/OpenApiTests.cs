using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Positions;
using DirectoryService.Core.Features.Departments.Commands.CreateDepartment;
using DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;
using DirectoryService.Core.Features.Locations.Commands.CreateLocation;
using DirectoryService.Core.Features.Locations.Commands.UpdateLocation;
using DirectoryService.Core.Features.Positions.Commands.CreatePosition;
using DirectoryService.Core.Features.Positions.Commands.UpdatePosition;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Core.Abstractions;
using Shared.Framework.Results;
using Shared.Kernel.Errors;
using CSharpFunctionalExtensions;

namespace DirectoryService.IntegrationTests.Documentation;

// This factory deliberately has no database: document generation must not need one.
public sealed class OpenApiWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICommandHandler<UpdatePositionCommand, Guid>>();
            services.AddScoped<ICommandHandler<UpdatePositionCommand, Guid>, ErrorHandler>();
        });
    }

    private sealed class ErrorHandler : ICommandHandler<UpdatePositionCommand, Guid>
    {
        public Task<Result<Guid, Error>> Handle(UpdatePositionCommand command, CancellationToken cancellationToken)
        {
            if (string.Equals(command.Dto.Name, "throw", StringComparison.Ordinal))
                throw new InvalidOperationException("Test exception");
            return Task.FromResult<Result<Guid, Error>>(Error.Internal(new ErrorMessage("internal.server.error", "Internal error")));
        }
    }
}

public sealed class OpenApiTests(OpenApiWebFactory factory) : IClassFixture<OpenApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Document_UsesCamelCaseForEveryParameter()
    {
        var document = await Document();
        foreach (var path in document["paths"]!.AsObject())
        {
            foreach (var operation in path.Value!.AsObject())
            {
                foreach (var parameter in operation.Value!["parameters"]?.AsArray() ?? [])
                {
                    var name = parameter!["name"]!.GetValue<string>();
                    Assert.True(char.IsLower(name[0]), $"{path.Key}: {name}");
                    if (string.Equals(parameter["in"]!.GetValue<string>(), "path", StringComparison.Ordinal))
                        Assert.Contains("{" + name + "}", path.Key, StringComparison.Ordinal);
                }
            }
        }

        var locationNames = document["paths"]!["/api/locations"]!["get"]!["parameters"]!.AsArray()
            .Select(p => p!["name"]!.GetValue<string>()).ToArray();
        Assert.Contains("pageSize", locationNames);
        Assert.Contains("sortBy", locationNames);
        Assert.Contains("sortDirection", locationNames);
        Assert.Contains("minDepartmentCount", locationNames);
        Assert.Contains(document["paths"]!["/api/departments/tree/search"]!["get"]!["parameters"]!.AsArray(),
            p => string.Equals(p!["name"]!.GetValue<string>(), "q", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Document_DescribesAllBusinessOperationsAndExamples()
    {
        var document = await Document();
        var operations = document["paths"]!.AsObject().SelectMany(p => p.Value!.AsObject()).ToArray();
        Assert.Equal(24, operations.Length);
        foreach (var operation in operations)
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.Value!["summary"]!.GetValue<string>()));
            Assert.False(string.IsNullOrWhiteSpace(operation.Value["description"]!.GetValue<string>()));
            foreach (var response in operation.Value["responses"]!.AsObject())
            {
                if (string.Equals(response.Key, "204", StringComparison.Ordinal))
                {
                    Assert.True(response.Value!["content"] is null || response.Value["content"]!.AsObject().Count == 0);
                    continue;
                }
                Assert.Single(response.Value!["content"]!.AsObject());
                var content = response.Value["content"]!["application/json"]!;
                Assert.NotNull(content["schema"]);
                Assert.True(content["example"] is not null || content["examples"]!.AsObject().Count > 0);
            }
        }
    }

    [Fact]
    public async Task Responses_UseTypedEnvelopesAndActualJsonShape()
    {
        var document = await Document();
        var created = Response(document, "/api/locations", "post", "201");
        Assert.NotNull(created["headers"]!["Location"]);
        var envelope = Resolve(document, created["content"]!["application/json"]!["schema"]!);
        var fields = envelope["properties"]!;
        Assert.Equal((string[])["error", "isError", "result", "timeGenerated"], fields.AsObject().Select(p => p.Key).Order(StringComparer.Ordinal));
        var location = Resolve(document, fields["result"]!);
        Assert.NotNull(location["properties"]!["address"]);
        Assert.Null(location["properties"]!["country"]);
        var address = Resolve(document, location["properties"]!["address"]!);
        Assert.True(IsNullable(address["properties"]!["region"]!));
        var sample = created["content"]!["application/json"]!["example"]!;
        var typed = sample.Deserialize<Envelope<LocationDto>>(JsonOptions);
        Assert.NotNull(typed!.Result!.Address);
        Assert.False(typed.IsError);
        Assert.Null(typed.Error);
        var errorType = Resolve(document, Resolve(document, fields["error"]!)["properties"]!["type"]!);
        Assert.Contains("Validation", errorType["enum"]!.AsArray().Select(n => n!.GetValue<string>()));
    }

    [Theory]
    [InlineData("/api/departments")]
    [InlineData("/api/locations")]
    public async Task PartialPatch_FieldsAreOptionalAndNullable(string path)
    {
        var document = await Document();
        var schema = RequestSchema(document, path + "/{id}", "patch");
        Assert.True(schema["required"] is null || schema["required"]!.AsArray().Count == 0);
        foreach (var property in schema["properties"]!.AsObject())
        {
            Assert.True(IsNullable(property.Value!), property.Key);
            Assert.False(string.IsNullOrWhiteSpace(property.Value!["description"]!.GetValue<string>()));
        }
    }

    [Fact]
    public async Task RequiredFields_FollowMvcRatherThanRecordConstructorParameters()
    {
        var document = await Document();
        AssertRequired(RequestSchema(document, "/api/departments", "post"), "locationIds", "name", "slug");
        AssertRequired(RequestSchema(document, "/api/locations", "post"), "city", "country", "houseNumber", "name", "street");
        AssertRequired(RequestSchema(document, "/api/positions/{id}", "patch"), "name");
        AssertRequired(RequestSchema(document, "/api/departments/{id}/parent", "put"));
        var locationPatch = RequestSchema(document, "/api/locations/{id}", "patch");
        Assert.Contains("empty string preserves", locationPatch["properties"]!["region"]!["description"]!.GetValue<string>(), StringComparison.Ordinal);
        var departmentPatch = RequestSchema(document, "/api/departments/{id}", "patch");
        Assert.Contains("Empty UUID: make root", departmentPatch["properties"]!["parentId"]!["description"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErrorResponses_IncludeBoth500FormatsAndOperationSpecificConditions()
    {
        var document = await Document();
        var conflict = Response(document, "/api/departments/{id}", "delete", "409");
        Assert.Contains("department.has.active.children", conflict["description"]!.GetValue<string>(), StringComparison.Ordinal);
        var error = Response(document, "/api/positions/{id}", "patch", "500")["content"]!["application/json"]!;
        Assert.Equal(2, error["schema"]!["oneOf"]!.AsArray().Count);
        Assert.NotNull(error["examples"]!["unhandled.exception"]!["value"]!["messages"]);
        Assert.Null(error["examples"]!["unhandled.exception"]!["value"]!["result"]);
        Assert.True(error["examples"]!["internal.server.error"]!["value"]!["isError"]!.GetValue<bool>());
        Assert.DoesNotContain("location.exists", Response(document, "/api/positions", "post", "409")["description"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequestExamples_PassActualValidators()
    {
        var document = await Document();
        Assert.True((await new CreateDepartmentValidator().ValidateAsync(Example<CreateDepartmentRequest>(document, "/api/departments", "post"))).IsValid);
        Assert.True((await new UpdateDepartmentValidator().ValidateAsync(Example<UpdateDepartmentRequest>(document, "/api/departments/{id}", "patch"))).IsValid);
        Assert.True((await new CreateLocationValidator().ValidateAsync(Example<CreateLocationRequest>(document, "/api/locations", "post"))).IsValid);
        Assert.True((await new UpdateLocationValidator().ValidateAsync(Example<UpdateLocationRequest>(document, "/api/locations/{id}", "patch"))).IsValid);
        Assert.True((await new CreatePositionValidator().ValidateAsync(Example<CreatePositionRequest>(document, "/api/positions", "post"))).IsValid);
        Assert.True((await new UpdatePositionValidator().ValidateAsync(Example<UpdatePositionRequest>(document, "/api/positions/{id}", "patch"))).IsValid);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"name\":null}")]
    public async Task RequiredPositionName_RejectsMissingAndNullWithDocumented400(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var response = await _client.PatchAsync("/api/positions/11111111-1111-1111-1111-111111111111", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.Equal("invalid.model.state", error!.Error!.Messages[0].Code);
    }

    [Theory]
    [InlineData("throw", false)]
    [InlineData("handled", true)]
    public async Task Actual500Responses_MatchDocumentedExamples(string name, bool wrapped)
    {
        using var response = await _client.PatchAsJsonAsync("/api/positions/11111111-1111-1111-1111-111111111111", new UpdatePositionRequest(name));
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>();
        Assert.Equal(wrapped, body!.ContainsKey("isError"));
        var error = wrapped ? body["error"]! : body;
        Assert.Equal("Internal", error["type"]!.GetValue<string>());
        Assert.Equal("internal.server.error", error["messages"]![0]!["code"]!.GetValue<string>());
    }

    [Fact]
    public async Task LivenessEndpoint_IsHealthy()
    {
        using var response = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<JsonObject> Document()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    private static JsonNode Response(JsonObject document, string path, string method, string status) => document["paths"]![path]![method]!["responses"]![status]!;

    private static JsonNode Resolve(JsonObject document, JsonNode schema)
    {
        if (schema["$ref"] is { } reference)
            return document["components"]!["schemas"]![reference.GetValue<string>().Split('/')[^1]]!;
        if (schema["oneOf"] is JsonArray alternatives)
            return Resolve(document, alternatives.First(n => n!["$ref"] is not null)!);
        return schema;
    }

    private static JsonNode RequestSchema(JsonObject document, string path, string method) =>
        Resolve(document, document["paths"]![path]![method]!["requestBody"]!["content"]!["application/json"]!["schema"]!);

    private static bool IsNullable(JsonNode schema) => schema["type"] is JsonArray types && types.Any(n => string.Equals(n!.GetValue<string>(), "null", StringComparison.Ordinal));

    private static void AssertRequired(JsonNode schema, params string[] expected) =>
        Assert.Equal(expected.Order(StringComparer.Ordinal), (schema["required"]?.AsArray().Select(n => n!.GetValue<string>()) ?? []).Order(StringComparer.Ordinal));

    private static T Example<T>(JsonObject document, string path, string method) => RequestSchema(document, path, method)["example"]!.Deserialize<T>(JsonOptions)!;
}
