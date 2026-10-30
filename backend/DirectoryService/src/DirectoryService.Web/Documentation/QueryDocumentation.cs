using System.Text.Json.Nodes;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
using DirectoryService.Core.Features.Locations.Queries.GetLocationsList;
using DirectoryService.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi;

namespace DirectoryService.Web.Documentation;

internal static class QueryDocumentation
{
    public static void Describe(OpenApiOperation operation, ControllerActionDescriptor action)
    {
        if (operation.Parameters is null)
            return;

        var controller = action.ControllerTypeInfo.AsType();
        if (controller == typeof(SearchController))
        {
            SearchDocumentation.Describe(operation);
            return;
        }
        var method = action.MethodInfo.Name;
        var departmentList = controller == typeof(DepartmentsController) && string.Equals(method, nameof(DepartmentsController.GetDepartmentList), StringComparison.Ordinal);
        var locationList = controller == typeof(LocationsController) && string.Equals(method, nameof(LocationsController.GetLocationList), StringComparison.Ordinal);
        var positionList = controller == typeof(PositionsController) && string.Equals(method, nameof(PositionsController.GetPositionList), StringComparison.Ordinal);
        var departmentPositions = controller == typeof(DepartmentsController) && string.Equals(method, nameof(DepartmentsController.GetDepartmentPositions), StringComparison.Ordinal);
        var treeSearch = controller == typeof(DepartmentsController) && string.Equals(method, nameof(DepartmentsController.GetDepartmentTreeByName), StringComparison.Ordinal);
        var departmentPage = controller == typeof(DepartmentsController) && method is
            nameof(DepartmentsController.GetDepartmentTree) or nameof(DepartmentsController.GetChildrenByParentId) or nameof(DepartmentsController.GetAncestorsById);

        foreach (var parameter in operation.Parameters.OfType<OpenApiParameter>())
        {
            if (parameter.In == ParameterLocation.Path)
            {
                parameter.Description = "Resource UUID. Empty UUID is syntactically valid; see this operation's errors for its meaning.";
                continue;
            }
            if (parameter.Schema is not OpenApiSchema schema)
                continue;
            if (departmentList || locationList || positionList || departmentPositions || treeSearch || departmentPage)
                DescribePagination(parameter, schema);
            if (departmentList || locationList || positionList || departmentPositions)
                DescribeList(parameter, schema, locationList);
            if ((locationList || positionList) && Is(parameter, nameof(GetPositionListRequest.IsActive)))
            {
                parameter.Description = "true returns active records; false returns inactive records, including soft-deleted and archived records. Default: true. Empty and null values return 400.";
                schema.Default = JsonValue.Create(true);
            }
            if (treeSearch && Is(parameter, nameof(GetDepartmentTreeByNameRequest.Q)))
            {
                parameter.Description = "Required case-insensitive ILIKE name search, at least 2 characters; % and _ are wildcards. No maximum length is validated.";
                parameter.Required = true;
                schema.MinLength = 2;
                schema.Example = JsonValue.Create("Engineering");
            }
            if (locationList && Is(parameter, nameof(GetLocationListQuery.MinDepartmentCount)))
            {
                parameter.Description = "Minimum stored department link count, inclusive; omitted means no count filter.";
                schema.Minimum = "0";
                schema.Example = JsonValue.Create(1);
            }
            if (locationList && Is(parameter, nameof(GetLocationListQuery.DepartmentIds)))
            {
                parameter.Description = "Optional comma-separated department UUIDs. Returns locations linked to at least one selected department, without duplicates. Omitted, empty or whitespace means no department filter. Combines with search and minDepartmentCount before pagination and totalCount; departmentCount still includes all stored links. Invalid UUIDs or empty list entries return 400.";
                schema.Example = JsonValue.Create("11111111-1111-1111-1111-111111111111,22222222-2222-2222-2222-222222222222");
            }
        }
    }

    // These rules are shared only by the explicitly selected operations above.
    private static void DescribePagination(OpenApiParameter parameter, OpenApiSchema schema)
    {
        if (Is(parameter, nameof(GetDepartmentTreeRequest.Page)))
        {
            parameter.Description = "Page number, starting at 1. Default: 1.";
            schema.Minimum = "1";
            schema.Default = JsonValue.Create(1);
        }
        else if (Is(parameter, nameof(GetDepartmentTreeRequest.PageSize)))
        {
            parameter.Description = "Items per page, 5-100 inclusive. Default: 20.";
            schema.Minimum = "5";
            schema.Maximum = "100";
            schema.Default = JsonValue.Create(20);
        }
    }

    private static void DescribeList(OpenApiParameter parameter, OpenApiSchema schema, bool locationList)
    {
        if (Is(parameter, nameof(GetPositionListRequest.Search)))
        {
            parameter.Description = "Optional case-insensitive ILIKE name search, at most 100 characters; % and _ are wildcards.";
            schema.MaxLength = 100;
            schema.Example = JsonValue.Create("Engineering");
        }
        else if (Is(parameter, nameof(GetDepartmentListQuery.SortBy)))
        {
            parameter.Description = "Case-sensitive sort field. Ties use ID ascending. Default: name.";
            schema.Enum = locationList
                ? [JsonValue.Create("name"), JsonValue.Create("createdAt"), JsonValue.Create("departmentCount")]
                : [JsonValue.Create("name"), JsonValue.Create("createdAt")];
            schema.Default = JsonValue.Create("name");
        }
        else if (Is(parameter, nameof(GetPositionListRequest.SortDirection)))
        {
            parameter.Description = "Case-sensitive sort direction. Default: asc.";
            schema.Enum = [JsonValue.Create("asc"), JsonValue.Create("desc")];
            schema.Default = JsonValue.Create("asc");
        }
    }

    private static bool Is(OpenApiParameter parameter, string member) => string.Equals(parameter.Name, member, StringComparison.OrdinalIgnoreCase);
}
