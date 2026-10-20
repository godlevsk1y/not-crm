using System.Globalization;
using System.Text.Json;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Shared.Framework.Results;
using Shared.Kernel.Errors;

namespace DirectoryService.Web.Documentation;

internal sealed class OperationDocumentation : IOpenApiOperationTransformer
{
    public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (operation.Parameters is not null)
        {
            foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
            {
                if (parameter.Name is { } name)
                    parameter.Name = JsonNamingPolicy.CamelCase.ConvertName(name);
            }
        }

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action || operation.Responses is null)
            return;

        var errors = ResourceErrors(action).ToList();
        // Model binding can fail for a body, query string or an unconstrained route parameter.
        if (context.Description.ParameterDescriptions.Any(p => p.Source == BindingSource.Body ||
                p.Source == BindingSource.Query || (p.Source == BindingSource.Path && p.RouteInfo?.Constraints?.Any() != true)))
        {
            errors.Insert(0, new ApiError(Error.BadRequest(new ErrorMessage("invalid.model.state", "The request is provided in the incorrect format.")),
                "Malformed JSON, missing required body/fields, or model binding failure after a route matches."));
        }
        if (!string.Equals(context.Description.HttpMethod, "GET", StringComparison.Ordinal))
        {
            errors.Add(new ApiError(DepartmentErrors.TransferConflict(),
                "PostgreSQL deadlock mapped during saving or commit. Despite its name, this code can occur on all writes."));
        }
        errors.Add(new ApiError(GeneralErrors.Internal(), "Unhandled exception (raw Error) or unmapped persistence failure (envelope). Internal details are not exposed."));

        QueryDocumentation.Describe(operation, action);
        DescribeSuccess(operation, context);

        var envelopeSchema = await context.GetOrCreateSchemaAsync(typeof(Envelope), cancellationToken: cancellationToken);
        var errorSchema = await context.GetOrCreateSchemaAsync(typeof(Error), cancellationToken: cancellationToken);
        // Replace inherited error declarations with the statuses actually reachable by this operation.
        foreach (var status in operation.Responses.Keys.Where(s => int.TryParse(s, CultureInfo.InvariantCulture, out var code) && code >= 400).ToArray())
            operation.Responses.Remove(status);
        foreach (var group in errors.GroupBy(e => e.Status, StringComparer.Ordinal))
        {
            var media = new OpenApiMediaType
            {
                Schema = string.Equals(group.Key, "500", StringComparison.Ordinal)
                    ? new OpenApiSchema { OneOf = [envelopeSchema, errorSchema] }
                    : envelopeSchema,
                Examples = new Dictionary<string, IOpenApiExample>(StringComparer.Ordinal),
            };
            foreach (var entry in group)
            {
                var name = entry.Error.Messages[0].Code;
                if (media.Examples.ContainsKey(name))
                    continue;
                media.Examples.Add(name, new OpenApiExample
                {
                    Summary = entry.Condition,
                    Value = ContractExamples.Json(Envelope.Failure(entry.Error) with { TimeGenerated = ContractExamples.Timestamp }),
                });
            }
            if (string.Equals(group.Key, "500", StringComparison.Ordinal))
                media.Examples.Add("unhandled.exception", new OpenApiExample { Summary = "Unexpected exception: raw Error, without an envelope.", Value = ContractExamples.Json(GeneralErrors.Internal()) });

            operation.Responses.Add(group.Key, new OpenApiResponse
            {
                Description = ApiError.Table(group),
                Content = new Dictionary<string, OpenApiMediaType>(StringComparer.Ordinal) { ["application/json"] = media },
            });
        }
    }

    private static ApiError[] ResourceErrors(ControllerActionDescriptor action)
    {
        var type = action.ControllerTypeInfo.AsType();
        if (type == typeof(DepartmentsController))
            return DepartmentsDocumentation.Errors(action.MethodInfo.Name);
        if (type == typeof(LocationsController))
            return LocationsDocumentation.Errors(action.MethodInfo.Name);
        if (type == typeof(PositionsController))
            return PositionsDocumentation.Errors(action.MethodInfo.Name);
        return [];
    }

    private static void DescribeSuccess(OpenApiOperation operation, OpenApiOperationTransformerContext context)
    {
        foreach (var response in context.Description.SupportedResponseTypes.Where(r => r.StatusCode is >= 200 and < 300))
        {
            if (!operation.Responses!.TryGetValue(response.StatusCode.ToString(CultureInfo.InvariantCulture), out var documented))
                continue;
            if (response.StatusCode == StatusCodes.Status204NoContent)
            {
                documented.Description = "Success. No response body.";
                documented.Content?.Clear();
                continue;
            }
            if (documented.Content is not null)
            {
                // EndpointResults uses TypedResults JSON writers, not MVC output negotiation.
                foreach (var contentType in documented.Content.Keys.Where(k => !string.Equals(k, "application/json", StringComparison.Ordinal)).ToArray())
                    documented.Content.Remove(contentType);
            }
            documented.Description = "Success. result contains the documented payload; error is null and isError is false.";
            if (response.StatusCode == StatusCodes.Status201Created && documented is OpenApiResponse created)
            {
                created.Headers = new Dictionary<string, IOpenApiHeader>(StringComparer.Ordinal)
                {
                    ["Location"] = new OpenApiHeader { Description = "URI emitted by this operation; see the operation description for details.", Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uri-reference" } },
                };
            }
            if (response.Type is not { IsGenericType: true } type || type.GetGenericTypeDefinition() != typeof(Envelope<>) ||
                !ContractExamples.Results.TryGetValue(type.GetGenericArguments()[0], out var payload) || documented.Content is null)
                continue;
            foreach (var media in documented.Content.Values)
                media.Example = ContractExamples.Json(Envelope.Ok(payload) with { TimeGenerated = ContractExamples.Timestamp });
        }
    }
}
