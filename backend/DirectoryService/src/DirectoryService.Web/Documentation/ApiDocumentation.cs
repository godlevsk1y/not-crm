using Microsoft.AspNetCore.OpenApi;

namespace DirectoryService.Web.Documentation;

internal static class ApiDocumentation
{
    public static void Configure(OpenApiOptions options)
    {
        // Schemas use serializer/MVC metadata, then documentation-only descriptions/examples.
        options.AddSchemaTransformer<SchemaDocumentation>();
        // Operation metadata from controllers is applied before this transformer.
        options.AddOperationTransformer<OperationDocumentation>();
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "DirectoryService REST API";
            document.Info.Description = "Organizational departments, locations and positions. No authentication is configured. " +
                "JSON uses camelCase. Successes and handled failures use { result, error, isError, timeGenerated }; " +
                "204 has no body. Unexpected exceptions return a raw Error (see the 500 response schemas/examples). " +
                "Branch on messages[].code; human-readable messages may contain dynamic IDs/names. " +
                "invalidField is a nullable server-side name, not necessarily the JSON field name. " +
                "Model binding precedes handler validation; validators aggregate value-object errors as Validation. " +
                "Unmatched routes, including failed UUID route constraints, return a bare 404 outside these error tables. " +
                "Unsupported request media types can return 415 ProblemDetails before the handler runs.";
            return Task.CompletedTask;
        });
    }
}
