using DirectoryService.Contracts.Search;

namespace DirectoryService.Core.Features.Search;

public interface ISearchRepository
{
    Task<SearchResultDto> SearchAsync(string query, int limitPerType, CancellationToken cancellationToken);

    Task<SearchPageDto> SearchPageAsync(
        string entityType, string query, int page, int pageSize, CancellationToken cancellationToken);
}
