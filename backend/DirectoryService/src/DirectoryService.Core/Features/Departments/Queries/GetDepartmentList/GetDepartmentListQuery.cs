using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;

public record GetDepartmentListQuery(
    string? Search,
    string SortBy = "name",
    string SortDirection = "asc",
    bool? IsActive = null,
    IReadOnlyList<Guid>? LocationIds = null,
    IReadOnlyList<Guid>? ExcludeIds = null,
    int Page = 1,
    int PageSize = 20
) : IQuery;
