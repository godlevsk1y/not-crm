using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Queries.GetLocationsList;

public record GetLocationListQuery : IQuery
{
    public string? Search { get; init; }

    public int? MinDepartmentCount { get; init; }

    public string SortBy { get; init; } = "name";

    public string SortDirection { get; init; } = "asc";

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}