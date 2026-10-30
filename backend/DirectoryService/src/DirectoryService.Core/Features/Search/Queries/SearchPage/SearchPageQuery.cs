using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Search.Queries.SearchPage;

public record SearchPageQuery(string EntityType, string? Q, int Page = 1, int PageSize = 20) : IQuery;
