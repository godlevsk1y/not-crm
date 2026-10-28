using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Queries.GetPositionList;

public record GetPositionListQuery(
    string? Search = null,
    string SortBy = "name",
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 20,
    bool IsActive = true
) : IQuery;
