namespace DirectoryService.Shared.Results;

public sealed record PagedResult<T>(
    IEnumerable<T> Results,
    int Page, 
    int PageSize, 
    int TotalCount
);