namespace DirectoryService.Contracts.WebApi.Positions;

public record GetPositionListRequest(
    string? Search = null,
    string SortBy = "name",
    string SortDirection = "asc",
    int Page = 1,
    int PageSize = 20,
    bool IsActive = true
);
