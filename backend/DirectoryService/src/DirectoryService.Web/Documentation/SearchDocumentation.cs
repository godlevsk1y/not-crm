using System.Text.Json.Nodes;
using DirectoryService.Core.Features.Search;
using DirectoryService.Web.Controllers;
using Microsoft.OpenApi;

namespace DirectoryService.Web.Documentation;

internal static class SearchDocumentation
{
    public static ApiError[] Errors(string action) => action switch
    {
        nameof(SearchController.Search) =>
        [
            new(SearchErrors.QueryInvalid(), "q is missing or contains fewer than 2 characters after trimming."),
            new(SearchErrors.LimitInvalid(), "limitPerType is outside 1–100."),
        ],
        nameof(SearchController.SearchPage) =>
        [
            new(SearchErrors.QueryInvalid(), "q is missing or contains fewer than 2 characters after trimming."),
            new(SearchErrors.PageInvalid(), "page is less than 1."),
            new(SearchErrors.PageSizeInvalid(), "pageSize is outside 1–100."),
            new(SearchErrors.EntityTypeNotFound(), "entityType is not departments, locations or positions (case-sensitive)."),
        ],
        _ => [],
    };

    public static void Describe(OpenApiOperation operation)
    {
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.Schema is not OpenApiSchema schema)
                continue;
            switch (parameter.Name)
            {
                case "entityType":
                    parameter.Description = "Directory type; unknown values return 404. Only the selected table is queried.";
                    schema.Enum = [JsonValue.Create("departments"), JsonValue.Create("locations"), JsonValue.Create("positions")];
                    break;
                case "q":
                    parameter.Description = "Required literal case-insensitive name substring, trimmed before validation and search. At least 2 characters after trimming; %, _ and ! are literal. Missing, empty, whitespace or one-character input returns 400.";
                    parameter.Required = true;
                    schema.MinLength = 2;
                    schema.Example = JsonValue.Create("раз");
                    break;
                case "limitPerType":
                case "pageSize":
                    parameter.Description = "Integer from 1 to 100 inclusive, default 20. Empty, noninteger and out-of-range values return 400. limitPerType applies independently to each type; pageSize must equal the initial limitPerType when continuing.";
                    schema.Minimum = "1";
                    schema.Maximum = "100";
                    schema.Default = JsonValue.Create(20);
                    break;
                case "page":
                    parameter.Description = "Page number starting at 1, default 1. Empty, noninteger, less than 1 or greater than Int32.MaxValue returns 400. Beyond the last page returns empty items and hasMore=false.";
                    schema.Minimum = "1";
                    schema.Maximum = "2147483647";
                    schema.Default = JsonValue.Create(1);
                    break;
            }
        }
    }
}
