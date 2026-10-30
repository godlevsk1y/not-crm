using System.Diagnostics;
using Dapper;
using DirectoryService.Contracts.Search;
using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Search;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class SearchRepository(IDbConnectionFactory connectionFactory) : ISearchRepository
{
    public async Task<SearchResultDto> SearchAsync(string query, int limitPerType, CancellationToken cancellationToken)
    {
        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var sql = $"{SelectFrom("departments")};{SelectFrom("locations")};{SelectFrom("positions")};";
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            sql, new { pattern = EscapePattern(query), limit = limitPerType + 1, offset = 0L },
            cancellationToken: cancellationToken));

        var departments = ToPage(await results.ReadAsync<SearchItemDto>(), limitPerType);
        var locations = ToPage(await results.ReadAsync<SearchItemDto>(), limitPerType);
        var positions = ToPage(await results.ReadAsync<SearchItemDto>(), limitPerType);
        return new SearchResultDto(departments, locations, positions);
    }

    public async Task<SearchPageDto> SearchPageAsync(
        string entityType, string query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var table = entityType switch
        {
            "departments" => "departments",
            "locations" => "locations",
            "positions" => "positions",
            _ => throw new UnreachableException(),
        };

        using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<SearchItemDto>(new CommandDefinition(
            SelectFrom(table),
            new { pattern = EscapePattern(query), limit = pageSize + 1, offset = (long)(page - 1) * pageSize },
            cancellationToken: cancellationToken));
        return ToPage(items, pageSize);
    }

    private static string SelectFrom(string table) => $"""
        SELECT id, name
        FROM {table}
        WHERE deleted_at IS NULL AND name ILIKE @pattern ESCAPE '!'
        ORDER BY name ASC, id ASC
        LIMIT @limit OFFSET @offset
        """;

    private static string EscapePattern(string query) =>
        $"%{query.Replace("!", "!!", StringComparison.Ordinal).Replace("%", "!%", StringComparison.Ordinal).Replace("_", "!_", StringComparison.Ordinal)}%";

    private static SearchPageDto ToPage(IEnumerable<SearchItemDto> items, int limit)
    {
        var rows = items.ToList();
        var hasMore = rows.Count > limit;
        if (hasMore)
            rows.RemoveAt(limit);
        return new SearchPageDto(rows, hasMore);
    }
}
