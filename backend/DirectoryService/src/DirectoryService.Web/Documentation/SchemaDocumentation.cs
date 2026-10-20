using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Shared.Framework.Results;
using Shared.Kernel.Errors;
using Shared.Kernel.Results;

namespace DirectoryService.Web.Documentation;

internal sealed class SchemaDocumentation(IApiDescriptionGroupCollectionProvider apiDescriptions, IModelMetadataProvider modelMetadata) : IOpenApiSchemaTransformer
{
    private readonly HashSet<Type> _bodyTypes = apiDescriptions.ApiDescriptionGroups.Items
        .SelectMany(g => g.Items).SelectMany(a => a.ParameterDescriptions)
        .Where(p => p.Source == BindingSource.Body).Select(p => p.Type).ToHashSet();

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var description = context.JsonPropertyInfo?.AttributeProvider?
            .GetCustomAttributes(typeof(DescriptionAttribute), inherit: true)
            .OfType<DescriptionAttribute>().FirstOrDefault();
        schema.Description = description?.Description ?? schema.Description;

        var typeInfo = context.JsonTypeInfo;
        if (ContractExamples.Requests.TryGetValue(typeInfo.Type, out var example))
            schema.Example = ContractExamples.Json(example);
        if (_bodyTypes.Contains(typeInfo.Type))
        {
            // Constructor parameters are optional in JSON, while MVC requires non-nullable references.
            var metadata = modelMetadata.GetMetadataForType(typeInfo.Type);
            schema.Required = typeInfo.Properties.Where(p => p.IsRequired ||
                    (!p.PropertyType.IsValueType && p.AttributeProvider is MemberInfo member &&
                        metadata.Properties.First(m => string.Equals(m.PropertyName, member.Name, StringComparison.Ordinal)).IsRequired))
                .Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        }
        DescribeSharedContract(schema, typeInfo);
        return Task.CompletedTask;
    }

    private static void DescribeSharedContract(OpenApiSchema schema, JsonTypeInfo typeInfo)
    {
        if (schema.Properties is null)
            return;
        var type = typeInfo.Type;

        if (type == typeof(Envelope) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Envelope<>)))
        {
            Describe(schema, typeInfo, nameof(Envelope.Result), "Success payload; null on handled failure.");
            Describe(schema, typeInfo, nameof(Envelope.Error), "Error details: category and messages.");
            Describe(schema, typeInfo, nameof(Envelope.IsError), "True when error is non-null.");
            Describe(schema, typeInfo, nameof(Envelope.TimeGenerated), "UTC time when this envelope was created.");
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(PagedResult<>))
        {
            Describe(schema, typeInfo, nameof(PagedResult<object>.Results), "Items in this page.");
            Describe(schema, typeInfo, nameof(PagedResult<object>.Page), "Requested page number, starting at 1.");
            Describe(schema, typeInfo, nameof(PagedResult<object>.PageSize), "Requested page size.");
            Describe(schema, typeInfo, nameof(PagedResult<object>.TotalCount), "Matching count before pagination, retained even when the requested page is empty.");
        }
        else if (type == typeof(Error))
        {
            Describe(schema, typeInfo, nameof(Error.Type), "Error category, serialized as a string.");
            Describe(schema, typeInfo, nameof(Error.Messages), "One or more errors; validation can aggregate multiple failures.");
        }
        else if (type == typeof(ErrorMessage))
        {
            Describe(schema, typeInfo, nameof(ErrorMessage.Code), "Stable machine-readable code; use for program logic.");
            Describe(schema, typeInfo, nameof(ErrorMessage.Message), "Human-readable explanation; may contain dynamic IDs/names.");
            Describe(schema, typeInfo, nameof(ErrorMessage.InvalidField), "Nullable server-side field name, e.g. DepartmentName, PageSize or Search (for query parameter q).");
        }
    }

    private static void Describe(OpenApiSchema schema, JsonTypeInfo typeInfo, string member, string description)
    {
        // Resolve the serialized name rather than assuming every contract uses camelCase.
        var name = typeInfo.Properties.First(p => p.AttributeProvider is MemberInfo info && string.Equals(info.Name, member, StringComparison.Ordinal)).Name;
        if (schema.Properties![name] is OpenApiSchema field)
            field.Description = description;
    }
}
