using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Search.Queries.Search;

public record SearchQuery(string? Q, int LimitPerType = 20) : IQuery;
