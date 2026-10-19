using System.Text.Json.Nodes;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Positions;
using DirectoryService.Contracts.WebApi.Departments;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DirectoryService.Web.Configuration;

// Documentation metadata only: do not add MVC validation attributes to transport contracts.
internal static class ApiDocumentation
{
    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "DirectoryService REST API";
            document.Info.Description = "Organizational departments, locations and positions. " +
                "JSON uses camelCase. Success and error bodies use an envelope " +
                "{ result, error, isError, timeGenerated }; 204 has no body. " +
                "Errors contain type and messages [{ code, message, invalidField }]. " +
                "Use code for program logic; invalidField is nullable and uses server-side field names. " +
                "Error codes and their conditions are documented under each operation. " +
                "Model binding runs before handler validation; validators aggregate value object errors as Validation. " +
                "Unmatched routes (including failed UUID route constraints) and responses without ErrorMessage are outside these error tables. " +
                "No authentication is configured.";
            return Task.CompletedTask;
        });
        options.AddOperationTransformer((operation, context, _) =>
        {
            DescribeParameters(operation, context);
            if (operation.Responses is not null)
            {
                foreach (var (status, response) in operation.Responses)
                {
                    response.Description = status switch
                    {
                        "200" => "Success. The result property contains the documented payload.",
                        "201" => "Created. The result property contains the resource; Location contains its URI.",
                        "204" => "Success. No response body.",
                        _ => response.Description,
                    };
                }
            }
            ApiErrorDocumentation.DescribeOperation(operation, context);
            return Task.CompletedTask;
        });
        options.AddSchemaTransformer((schema, context, _) =>
        {
            DescribeSchema(schema, context.JsonTypeInfo.Type);
            return Task.CompletedTask;
        });
    }

    private static void DescribeParameters(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        if (operation.Parameters is null)
            return;

        var isLocation = context.Description.RelativePath?.StartsWith("api/Locations", StringComparison.OrdinalIgnoreCase) == true;
        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            if (parameter.Schema is not OpenApiSchema schema)
                continue;

            if (parameter.In == ParameterLocation.Path)
            {
                parameter.Description = "Resource ID in UUID format. Required.";
                continue;
            }

            switch (parameter.Name?.ToUpperInvariant())
            {
                case "PAGE":
                    parameter.Description = "Page number, starting at 1. Default: 1.";
                    schema.Minimum = "1";
                    schema.Default = JsonValue.Create(1);
                    break;
                case "PAGESIZE":
                    parameter.Description = "Items per page, 5–100. Default: 20.";
                    schema.Minimum = "5";
                    schema.Maximum = "100";
                    schema.Default = JsonValue.Create(20);
                    break;
                case "SEARCH":
                    parameter.Description = "Optional case-insensitive name search, at most 100 characters. ILIKE wildcards % and _ are supported.";
                    schema.MaxLength = 100;
                    break;
                case "Q":
                    parameter.Description = "Required case-insensitive name search, at least 2 characters. ILIKE wildcards % and _ are supported.";
                    schema.MinLength = 2;
                    parameter.Required = true;
                    break;
                case "MINDEPARTMENTCOUNT":
                    parameter.Description = "Minimum department link count (inclusive). Omitted means 0.";
                    schema.Minimum = "0";
                    break;
                case "SORTBY":
                    parameter.Description = "Sort field (case-sensitive). Ties are ordered by ID. Default: name.";
                    schema.Enum = isLocation
                        ? [JsonValue.Create("name"), JsonValue.Create("createdAt"), JsonValue.Create("departmentCount")]
                        : [JsonValue.Create("name"), JsonValue.Create("createdAt")];
                    schema.Default = JsonValue.Create("name");
                    break;
                case "SORTDIRECTION":
                    parameter.Description = "Sort direction (case-sensitive). Default: asc.";
                    schema.Enum = [JsonValue.Create("asc"), JsonValue.Create("desc")];
                    schema.Default = JsonValue.Create("asc");
                    break;
            }
        }
    }

    private static void DescribeSchema(OpenApiSchema schema, Type type)
    {
        if (schema.Properties is null)
            return;

        var isRequest = type.Name.EndsWith("Request", StringComparison.Ordinal);
        var isPatch = type == typeof(UpdateDepartmentRequest) || type == typeof(UpdateLocationRequest);
        if (isRequest)
        {
            // Constructor parameters without C# defaults are not necessarily required by MVC.
            string[] requiredFields = type.Name switch
            {
                nameof(CreateDepartmentRequest) => ["name", "slug", "locationIds"],
                nameof(CreateLocationRequest) => ["name", "country", "city", "street", "houseNumber"],
                nameof(CreatePositionRequest) or nameof(UpdatePositionRequest) => ["name"],
                _ => [],
            };
            schema.Required = new HashSet<string>(requiredFields, StringComparer.Ordinal);
        }

        foreach (var (name, property) in schema.Properties)
        {
            if (property is not OpenApiSchema field)
                continue;

            field.Description = name switch
            {
                "id" => "Resource UUID.",
                "name" when isRequest => "Must contain non-whitespace text; trimmed before saving. At most 100 characters after trimming." +
                    (isPatch ? " Null or omitted: unchanged." : " Required."),
                "name" => "Resource name.",
                "slug" => "Lowercase letters a–z, digits and hyphens; no leading/trailing hyphen. No application-level length limit; the database allows 100 characters (longer values fail with 500).",
                "locationIds" => "Required array of distinct existing location UUIDs; [] is allowed. Unknown IDs return 404. Duplicate IDs are not supported.",
                "parentId" when type == typeof(UpdateDepartmentRequest) => "Null/omitted: unchanged. Empty UUID: make root. Other UUID: existing parent. Only this department path is recalculated.",
                "parentId" => "Parent UUID; null means a root department.",
                "newParentId" => "New parent UUID. Null or omitted makes a root. Unlike PATCH, the empty UUID is looked up as a parent and returns 404. Transfers the whole subtree.",
                "path" => "Hierarchy path with dot-separated slugs, e.g. company.engineering. Root path is its slug.",
                "depth" => "Depth from root; roots have depth 0.",
                "country" or "city" or "street" or "houseNumber" => "Non-whitespace address component." + (isPatch ? " Null/omitted: unchanged." : " Required on creation."),
                "region" or "district" or "postalCode" => "Optional address component; null is allowed." + (isPatch ? " Null, omitted or empty string preserves the existing value; clearing is not supported." : ""),
                "address" => "Nested address in responses. Creation and update requests use flat address fields.",
                "createdAt" => "Creation timestamp in UTC.",
                "hasChildren" => "Whether there are active immediate children.",
                "childCount" => "Number of active immediate children.",
                "departmentCount" => "Number of stored department/location links; may include links from soft-deleted departments until cleanup.",
                "ancestors" => "All active ancestors, ordered from root to parent; excludes the matching department.",
                "results" => "Items in this page.",
                "page" => "Requested page number (starts at 1).",
                "pageSize" => "Requested page size.",
                "totalCount" => "Matching count before pagination. SQL-backed lists/tree queries return 0 when the requested page is empty.",
                "result" => "Success payload; null on error.",
                "error" => "Null on success; otherwise contains type and messages.",
                "isError" => "True when error is non-null.",
                "timeGenerated" => "UTC time when the response envelope was created.",
                "code" => "Machine-readable error code. Use this for program logic. See Error codes under this operation and the applicable response descriptions.",
                "message" => "Human-readable explanation.",
                "invalidField" => "Nullable server-side field name (for example PageSize or DepartmentName); not necessarily the JSON property name.",
                "type" => "Error category serialized as a string: Validation, Domain, BadRequest, NotFound, Conflict, Failure or Internal.",
                "messages" => "One or more errors, including field-specific errors where available.",
                _ => field.Description,
            };
            if (isRequest && name is "name" or "country" or "city" or "street" or "houseNumber")
                field.MinLength = 1;
            if (name is "country" or "region" or "city" or "district" or "street" or "houseNumber")
                field.Description += " Database limit: 60 characters; exceeding it returns 500 (not a validation error).";
            if (string.Equals(name, "postalCode", StringComparison.Ordinal))
                field.Description += " Database limit: 10 characters; exceeding it returns 500 (not a validation error).";
            if (string.Equals(name, "slug", StringComparison.Ordinal) && isRequest)
                field.Pattern = "^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$";
        }

        var example = type.Name switch
        {
            nameof(CreateLocationRequest) => """{"name":"Moscow Office","country":"Russia","city":"Moscow","street":"Tverskaya","houseNumber":"1"}""",
            nameof(CreateDepartmentRequest) => """{"name":"Engineering","slug":"engineering","locationIds":[],"parentId":null}""",
            nameof(CreatePositionRequest) or nameof(UpdatePositionRequest) => """{"name":"Engineer"}""",
            nameof(UpdateDepartmentRequest) => """{"name":"Engineering team"}""",
            nameof(UpdateLocationRequest) => """{"city":"Saint Petersburg"}""",
            nameof(TransferDepartmentRequest) => """{"newParentId":null}""",
            _ => null,
        };
        if (example is not null)
            schema.Example = JsonNode.Parse(example);
    }
}
