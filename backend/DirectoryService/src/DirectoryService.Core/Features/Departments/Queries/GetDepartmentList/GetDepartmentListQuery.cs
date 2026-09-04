using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;

public record GetDepartmentListQuery(
    string? Search,
    string SortBy = "name",
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 20
) : IQuery;
