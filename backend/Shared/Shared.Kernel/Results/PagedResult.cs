namespace Shared.Kernel.Results;

public sealed record PagedResult<T>(
    IEnumerable<T> Results,
    int Page, 
    int PageSize, 
    long TotalCount
);