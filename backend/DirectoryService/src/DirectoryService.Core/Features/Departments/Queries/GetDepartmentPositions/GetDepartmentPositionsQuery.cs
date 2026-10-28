using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentPositions;

public record GetDepartmentPositionsQuery(
    Guid DepartmentId,
    string SortBy = "name",
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 20
) : IQuery;
