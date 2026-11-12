using Microsoft.AspNetCore.OpenApi;

namespace EmployeeService.Web.Documentation;

internal static class ApiDocumentation
{
    public static void Configure(OpenApiOptions options)
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info.Title = "EmployeeService REST API";
            document.Info.Description = "Employee records management.";
            return Task.CompletedTask;
        });
    }
}
