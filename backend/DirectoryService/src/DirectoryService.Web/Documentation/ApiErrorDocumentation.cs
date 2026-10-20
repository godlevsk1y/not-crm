using System.Text.Json.Nodes;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Core.Features.Positions;
using DirectoryService.Domain.Models.Errors;
using DirectoryService.Domain.ValueObjects.Errors;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Shared.Kernel.Errors;

namespace DirectoryService.Web.Documentation;

// Update this catalogue when an error or its reachable operations change.
// Describe the final response type: validators flatten domain errors into Validation.
// MVC rejects null LocationIds before the validator, so department.locations.null is unreachable over REST.
internal static class ApiErrorDocumentation
{
    private static readonly string[] Departments =
    [
        "POST /api/departments",
        "PATCH /api/departments/{id}",
    ];

    private static readonly string[] Locations =
    [
        "POST /api/locations",
        "PATCH /api/locations/{id}",
    ];

    private static readonly string[] Positions =
    [
        "POST /api/positions",
        "PATCH /api/positions/{id}",
    ];

    private static readonly string[] DepartmentPages =
    [
        "GET /api/departments",
        "GET /api/departments/tree",
        "GET /api/departments/{id}/children",
        "GET /api/departments/{id}/ancestors",
        "GET /api/departments/tree/search",
    ];

    private static readonly Guid ExampleId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed record Entry(Error Error, string Condition, IReadOnlyCollection<string> Operations)
    {
        public string Status => Error.Type switch
        {
            ErrorType.Validation or ErrorType.Domain or ErrorType.BadRequest => "400",
            ErrorType.NotFound => "404",
            ErrorType.Conflict => "409",
            _ => "500",
        };
    }

    private static Error Validation(Error error) => Error.Validation(error.Messages);

    private static readonly Entry[] Catalogue =
    [
        new(Error.BadRequest(new ErrorMessage("invalid.model.state", "The request is provided in the incorrect format.")),
            "Malformed JSON, missing required body/fields, or model binding failure after a route matches.",
            [
                ..Departments,
                ..Locations,
                ..Positions,
                ..DepartmentPages,
                "GET /api/locations",
                "GET /api/positions",
                "PUT /api/departments/{id}/parent",
            ]),
        new(GeneralErrors.Internal(),
            "Unexpected exception or unmapped failure. Internal details are not exposed.", ["*"]),
        new(Validation(ValueObjectErrors.DepartmentName.Empty()),
            "Name is empty or whitespace (PATCH validates only supplied non-null names).", Departments),
        new(Validation(ValueObjectErrors.DepartmentName.TooLong()),
            "Trimmed name exceeds 100 characters (PATCH validates only supplied non-null names).", Departments),
        new(Validation(ValueObjectErrors.LocationName.Empty()),
            "Name is empty or whitespace (PATCH validates only supplied non-null names).", Locations),
        new(Validation(ValueObjectErrors.LocationName.TooLong()),
            "Trimmed name exceeds 100 characters (PATCH validates only supplied non-null names).", Locations),
        new(Validation(ValueObjectErrors.PositionName.Empty()),
            "Name is empty or whitespace; name is required on creation and PATCH.", Positions),
        new(Validation(ValueObjectErrors.PositionName.TooLong()),
            "Trimmed name exceeds 100 characters; name is required on creation and PATCH.", Positions),
        new(Validation(ValueObjectErrors.Slug.Empty()),
            "Slug is empty or whitespace; PATCH validates only supplied non-null values.", Departments),
        new(Validation(ValueObjectErrors.Slug.Invalid()),
            "Slug does not match ^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$; no trimming is performed.", Departments),
        new(Error.Validation(ValueObjectErrors.Address.CountryEmptyMessage),
            "Country is empty or whitespace on creation.", ["POST /api/locations"]),
        new(Error.Domain(ValueObjectErrors.Address.CountryEmptyMessage),
            "Supplied Country is empty or whitespace on update; null preserves its stored value.", ["PATCH /api/locations/{id}"]),
        new(Error.Validation(ValueObjectErrors.Address.CityEmptyMessage),
            "City is empty or whitespace on creation.", ["POST /api/locations"]),
        new(Error.Domain(ValueObjectErrors.Address.CityEmptyMessage),
            "Supplied City is empty or whitespace on update; null preserves its stored value.", ["PATCH /api/locations/{id}"]),
        new(Error.Validation(ValueObjectErrors.Address.StreetEmptyMessage),
            "Street is empty or whitespace on creation.", ["POST /api/locations"]),
        new(Error.Domain(ValueObjectErrors.Address.StreetEmptyMessage),
            "Supplied Street is empty or whitespace on update; null preserves its stored value.", ["PATCH /api/locations/{id}"]),
        new(Error.Validation(ValueObjectErrors.Address.HouseNumberEmptyMessage),
            "HouseNumber is empty or whitespace on creation.", ["POST /api/locations"]),
        new(Error.Domain(ValueObjectErrors.Address.HouseNumberEmptyMessage),
            "Supplied HouseNumber is empty or whitespace on update; null preserves its stored value.", ["PATCH /api/locations/{id}"]),
        new(Error.Validation(new ErrorMessage("department.id.empty", "Department Id cannot be empty", "DepartmentId")),
            "Department route ID is the empty UUID.", ["PUT /api/departments/{id}/parent"]),
        new(Error.Validation(new ErrorMessage("departments.page.invalid", "The page parameter must be greater than 0.", "Page")),
            "Page is less than 1.", DepartmentPages),
        new(Error.Validation(new ErrorMessage("departments.page.size.invalid", "The page size must be from 5 to 100.", "PageSize")),
            "PageSize is outside 5–100.", DepartmentPages),
        new(Error.Validation(new ErrorMessage("departments.search.too.long", "The search string must not exceed 100 characters.", "Search")),
            "Search exceeds 100 characters.", ["GET /api/departments"]),
        new(Error.Validation(new ErrorMessage("departments.sort.by.invalid", "The sortBy parameter only allows 'name' or 'createdAt' values.", "SortBy")),
            "SortBy is not name or createdAt (case-sensitive).", ["GET /api/departments"]),
        new(Error.Validation(new ErrorMessage("departments.sort.direction.invalid", "The sortBy parameter only allows 'asc' or 'desc' values.", "SortDirection")),
            "SortDirection is not asc or desc (case-sensitive).", ["GET /api/departments"]),
        new(Error.Validation(new ErrorMessage("positions.page.invalid", "The page parameter must be greater than 0.", "Page")),
            "Page is less than 1.", ["GET /api/positions"]),
        new(Error.Validation(new ErrorMessage("positions.page.size.invalid", "The page size must be from 5 to 100.", "PageSize")),
            "PageSize is outside 5–100.", ["GET /api/positions"]),
        new(Error.Validation(new ErrorMessage("positions.search.too.long", "The search string must not exceed 100 characters.", "Search")),
            "Search exceeds 100 characters.", ["GET /api/positions"]),
        new(Error.Validation(new ErrorMessage("positions.sort.by.invalid", "The sortBy parameter only allows 'name' or 'createdAt' values.", "SortBy")),
            "SortBy is not name or createdAt (case-sensitive).", ["GET /api/positions"]),
        new(Error.Validation(new ErrorMessage("positions.sort.direction.invalid", "The sortDirection parameter only allows 'asc' or 'desc' values.", "SortDirection")),
            "SortDirection is not asc or desc (case-sensitive).", ["GET /api/positions"]),
        new(Error.Validation(new ErrorMessage("departments.search.query.invalid", "Search query must be at least 2 characters long.", "Search")),
            "Search (query parameter q) has fewer than 2 characters. Omitted q can instead fail model binding.", ["GET /api/departments/tree/search"]),
        new(Error.Validation(new ErrorMessage("location.search.too.long", "The search string must not exceed 100 characters.", "Search")),
            "Search exceeds 100 characters.", ["GET /api/locations"]),
        new(Error.Validation(new ErrorMessage("location.department.count.invalid", "The minimum number of departments must be greater than or equal to zero.", "MinDepartmentCount")),
            "Supplied MinDepartmentCount is negative.", ["GET /api/locations"]),
        new(Error.Validation(new ErrorMessage("locations.sort.by.invalid", "The sortBy parameter only allows 'name', 'createdAt' or 'departmentCount' values.", "SortBy")),
            "SortBy is not name, createdAt or departmentCount (case-sensitive).", ["GET /api/locations"]),
        new(Error.Validation(new ErrorMessage("locations.sort.direction.invalid", "The sortBy parameter only allows 'asc' or 'desc' values.", "SortDirection")),
            "SortDirection is not asc or desc (case-sensitive).", ["GET /api/locations"]),
        new(Error.Validation(new ErrorMessage("locations.page.invalid", "The page parameter must be greater than 0.", "Page")),
            "Page is less than 1.", ["GET /api/locations"]),
        new(Error.Validation(new ErrorMessage("locations.page.size.invalid", "The page size must be from 5 to 100.", "PageSize")),
            "PageSize is outside 5–100.", ["GET /api/locations"]),
        new(ModelErrors.Department.ParentToItself(),
            "PATCH parentId equals the department ID.", ["PATCH /api/departments/{id}"]),
        new(DepartmentErrors.NotFound(ExampleId),
            "Requested parentId does not identify an active department, or the parent is removed before saving.", ["POST /api/departments"]),
        new(DepartmentErrors.NotFound(ExampleId),
            "Active department or requested parentId is missing, including a parent foreign key race while saving.", ["PATCH /api/departments/{id}"]),
        new(DepartmentErrors.NotFound(ExampleId),
            "Active department with the route ID was not found.",
            [
                "GET /api/departments/{id}",
                "DELETE /api/departments/{id}",
                "PUT /api/departments/{id}/parent",
            ]),
        new(DepartmentErrors.NotFound(ExampleId),
            "Active department with departmentId is missing, or is removed before the relationship is saved.",
            [
                "POST /api/departments/{departmentId}/locations/{locationId}",
                "POST /api/departments/{departmentId}/positions/{positionId}",
            ]),
        new(LocationErrors.NotFound(ExampleId),
            "A locationIds entry does not identify an active location, or a location is removed before saving the relationships.", ["POST /api/departments"]),
        new(LocationErrors.NotFound(ExampleId),
            "Active location with the route ID was not found.",
            [
                "GET /api/locations/{id}",
                "PATCH /api/locations/{id}",
                "DELETE /api/locations/{id}",
            ]),
        new(LocationErrors.NotFound(ExampleId),
            "Active location with locationId is missing, or is removed before the relationship is saved.", ["POST /api/departments/{departmentId}/locations/{locationId}"]),
        new(PositionErrors.NotFound(ExampleId),
            "Active position is missing, or a position foreign key race occurs while linking.",
            [
                "PATCH /api/positions/{id}",
                "DELETE /api/positions/{id}",
                "POST /api/departments/{departmentId}/positions/{positionId}",
            ]),
        new(LocationErrors.AlreadyExists("Moscow Office"),
            "Name duplicates an existing stored location, including a unique-constraint race; PATCH detects this on save.", Locations),
        new(DepartmentErrors.LocationAlreadyAdded(ExampleId, ExampleId),
            "The relationship already exists, including a concurrent insert detected by the unique constraint.", ["POST /api/departments/{departmentId}/locations/{locationId}"]),
        new(DepartmentErrors.DepartmentLocationNotFound(ExampleId, ExampleId),
            "The relationship does not exist; resource existence is not checked separately.", ["DELETE /api/departments/{departmentId}/locations/{locationId}"]),
        new(DepartmentErrors.PositionAlreadyAdded(ExampleId, ExampleId),
            "The relationship already exists, including a concurrent insert detected by the unique constraint.", ["POST /api/departments/{departmentId}/positions/{positionId}"]),
        new(DepartmentErrors.DepartmentPositionNotFound(ExampleId, ExampleId),
            "The relationship does not exist; resource existence is not checked separately.", ["DELETE /api/departments/{departmentId}/positions/{positionId}"]),
        new(DepartmentErrors.HasActiveChildren(ExampleId),
            "Department has at least one active immediate child.", ["DELETE /api/departments/{id}"]),
        new(DepartmentErrors.ParentNotFound(ExampleId),
            "Requested new parent is missing (including the empty UUID).", ["PUT /api/departments/{id}/parent"]),
        new(DepartmentErrors.ParentToSelf(),
            "newParentId equals the department ID.", ["PUT /api/departments/{id}/parent"]),
        new(DepartmentErrors.Cycle(ExampleId, ExampleId),
            "Requested parent is a descendant of the department.", ["PUT /api/departments/{id}/parent"]),
        new(DepartmentErrors.TransferConflict(),
            "PostgreSQL deadlock mapped by SaveChangesAsync or transaction commit; transfer also maps deadlocks from its transactional SQL. Despite its name this code can occur on other writes.",
            [
                ..Departments,
                ..Locations,
                ..Positions,
                "DELETE /api/departments/{id}",
                "PUT /api/departments/{id}/parent",
                "POST /api/departments/{departmentId}/locations/{locationId}",
                "POST /api/departments/{departmentId}/positions/{positionId}",
                "DELETE /api/departments/{departmentId}/locations/{locationId}",
                "DELETE /api/departments/{departmentId}/positions/{positionId}",
                "DELETE /api/locations/{id}",
                "DELETE /api/positions/{id}",
            ]),
    ];

    public static void DescribeOperation(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        var key = context.Description.HttpMethod + " /" + context.Description.RelativePath;
        var entries = Catalogue.Where(e =>
            e.Operations.Contains("*", StringComparer.Ordinal) ||
            e.Operations.Contains(key, StringComparer.OrdinalIgnoreCase)).ToArray();
        operation.Description += "\n\n### Error codes\n\n" +
            "Clients should branch on `code`; `message` is illustrative and may contain dynamic IDs or names. " +
            "`invalidField` is a nullable server-side name, not necessarily the JSON name (e.g. LocationName versus name, Search versus q). " +
            "\n\n" +
            Table(entries);

        if (operation.Responses is null)
            return;

        var envelopeContent = operation.Responses["400"].Content;
        if (!entries.Any(e => string.Equals(e.Status, "400", StringComparison.Ordinal)))
            operation.Responses.Remove("400");

        foreach (var group in entries.GroupBy(e => e.Status, StringComparer.Ordinal))
        {
            // Reuse the existing envelope schema for statuses omitted by controller metadata.
            if (!operation.Responses.TryGetValue(group.Key, out var response))
            {
                response = new OpenApiResponse
                {
                    Content = envelopeContent?.ToDictionary(
                        p => p.Key, p => new OpenApiMediaType { Schema = p.Value.Schema }, StringComparer.Ordinal),
                };
                operation.Responses.Add(group.Key, response);
            }
            response.Description = "Applicable error codes for this operation.\n\n" + Table(group);
            if (response.Content is null || !response.Content.TryGetValue("application/json", out var mediaType))
                continue;

            mediaType.Example = null;
            mediaType.Examples = new Dictionary<string, IOpenApiExample>(StringComparer.Ordinal);
            foreach (var typeGroup in group.GroupBy(e => e.Error.Type))
            {
                var sample = typeGroup.First();
                // Multiple validation failures are returned together; other samples illustrate one failure.
                var messages = typeGroup.Key == ErrorType.Validation
                    ? typeGroup.SelectMany(e => e.Error.Messages).DistinctBy(m => m.InvalidField, StringComparer.Ordinal).Take(2)
                    : sample.Error.Messages;
                mediaType.Examples.Add(typeGroup.Key.ToString(), new OpenApiExample
                {
                    Summary = typeGroup.Key.ToString(),
                    Value = Example(typeGroup.Key, messages),
                });
            }
        }
    }

    private static string Table(IEnumerable<Entry> entries)
    {
        const string header = "| code | HTTP | error.type | Condition | invalidField | Example message |\n" +
            "| --- | --- | --- | --- | --- | --- |\n";
        return header + string.Join("\n", entries.SelectMany(entry => entry.Error.Messages.Select(message =>
            $"| `{message.Code}` | {entry.Status} | {entry.Error.Type} | {entry.Condition} | " +
            $"{(message.InvalidField is null ? "`null`" : $"`{message.InvalidField}`")} | {message.Message} |")));
    }

    private static JsonObject Example(ErrorType type, IEnumerable<ErrorMessage> messages) => new()
    {
        ["result"] = null,
        ["error"] = new JsonObject
        {
            ["messages"] = new JsonArray(messages.Select(message => (JsonNode)new JsonObject
            {
                ["code"] = message.Code,
                ["message"] = message.Message,
                ["invalidField"] = message.InvalidField,
            }).ToArray()),
            ["type"] = type.ToString(),
        },
        ["isError"] = true,
        ["timeGenerated"] = "2026-10-17T09:00:00Z",
    };
}
