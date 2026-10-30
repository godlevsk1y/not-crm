namespace DirectoryService.Contracts.Search;

public record SearchResultDto(SearchPageDto Departments, SearchPageDto Locations, SearchPageDto Positions);
