namespace DirectoryService.Contracts.WebApi.Departments;

public record GetDepartmentPositionsRequest
{
    public string SortBy { get; init; } = "name";

    public string SortDirection { get; init; } = "asc";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
