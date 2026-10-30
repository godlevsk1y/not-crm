using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using DirectoryService.Contracts.Search;
using DirectoryService.Core.Database;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;

namespace DirectoryService.IntegrationTests.Search;

public class SearchTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private static readonly string[] EntityTypes = ["departments", "locations", "positions"];
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public SearchTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_ShouldReturnOnlyActiveLiteralMatches_WithMinimalContract()
    {
        foreach (var type in EntityTypes)
        {
            await SeedAsync(type, "Отдел Разработки");
            await SeedAsync(type, "Разработка inactive", deleted: true);
            await SeedAsync(type, "Unrelated");
        }

        using var response = await _client.GetAsync("api/search?q=%20%20рАз%20%20");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.False(json.RootElement.GetProperty("isError").GetBoolean());
        var result = json.RootElement.GetProperty("result");
        Assert.Equal(EntityTypes.Order(StringComparer.Ordinal), result.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        foreach (var type in EntityTypes)
        {
            var block = result.GetProperty(type);
            Assert.False(block.GetProperty("hasMore").GetBoolean());
            var item = Assert.Single(block.GetProperty("items").EnumerateArray());
            Assert.Equal(["id", "name"], item.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
            Assert.Equal("Отдел Разработки", item.GetProperty("name").GetString());
            var page = await GetAsync<SearchPageDto>($"api/search/{type}?q=%20рАз%20");
            Assert.Equal("Отдел Разработки", Assert.Single(page.Items).Name);
            Assert.False(page.HasMore);
        }
    }

    [Theory]
    [InlineData("departments")]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task Search_ShouldReturnMatchesOnlyForSelectedType(string entityType)
    {
        await SeedAsync(entityType, "Engineering");
        var result = await GetAsync<SearchResultDto>("api/search?q=gInEeR");
        foreach (var (type, page) in Blocks(result))
        {
            Assert.Equal(string.Equals(type, entityType, StringComparison.Ordinal) ? 1 : 0, page.Items.Count);
            Assert.False(page.HasMore);
        }
    }

    [Theory]
    [InlineData("zz")]
    [InlineData("Engineering")]
    public async Task Search_ShouldReturnEmptyBlocks_WhenNoMatches(string query)
    {
        await SeedAsync("positions", "Other");
        var result = await GetAsync<SearchResultDto>($"api/search?q={query}");
        foreach (var (_, block) in Blocks(result))
        {
            Assert.Empty(block.Items);
            Assert.False(block.HasMore);
        }
    }

    [Theory]
    [InlineData("a%")]
    [InlineData("a_")]
    [InlineData("a!")]
    [InlineData("ab")]
    [InlineData("a\\")]
    [InlineData("!%_")]
    public async Task Search_ShouldEscapeLiteralPatternCharacters(string query)
    {
        foreach (var type in EntityTypes)
        {
            await SeedAsync(type, $"Prefix {query} suffix");
            await SeedAsync(type, "Prefix abc suffix");
            await SeedAsync(type, "Prefix a%_ suffix");
        }
        var encoded = Uri.EscapeDataString(query);
        var combined = await GetAsync<SearchResultDto>($"api/search?q={encoded}");
        foreach (var (type, block) in Blocks(combined))
        {
            var page = await GetAsync<SearchPageDto>($"api/search/{type}?q={encoded}");
            Assert.Equal(block.Items, page.Items);
            Assert.All(block.Items, item => Assert.Contains(query, item.Name, StringComparison.Ordinal));
            Assert.Contains(block.Items, item => string.Equals(item.Name, $"Prefix {query} suffix", StringComparison.Ordinal));
            Assert.False(block.HasMore);
        }
    }

    [Theory]
    [InlineData(20, "")]
    [InlineData(1, "&limitPerType=1")]
    [InlineData(3, "&limitPerType=3")]
    [InlineData(100, "&limitPerType=100")]
    public async Task Search_ShouldApplyIndependentLimits_WithoutRedistribution(int limit, string parameter)
    {
        foreach (var type in EntityTypes)
            for (var i = 0; i < (string.Equals(type, "locations", StringComparison.Ordinal) ? 1 : limit + 1); i++)
                await SeedAsync(type, $"Match {i:D3}");

        var result = await GetAsync<SearchResultDto>($"api/search?q=match{parameter}");
        Assert.Equal(limit, result.Departments.Items.Count);
        Assert.Equal(limit, result.Positions.Items.Count);
        Assert.True(result.Departments.HasMore);
        Assert.True(result.Positions.HasMore);
        Assert.Single(result.Locations.Items);
        Assert.False(result.Locations.HasMore);
        foreach (var (type, block) in Blocks(result))
        {
            var page = await GetAsync<SearchPageDto>($"api/search/{type}?q=match&pageSize={limit}");
            Assert.Equal(block.Items, page.Items);
            Assert.Equal(block.HasMore, page.HasMore);
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public async Task Search_ShouldFetchExtraRowOnlyForHasMore(int count, bool hasMore)
    {
        foreach (var type in EntityTypes)
            for (var i = 0; i < count; i++)
                await SeedAsync(type, $"Match {i:D3}");

        var combined = await GetAsync<SearchResultDto>("api/search?q=match&limitPerType=3");
        foreach (var (type, block) in Blocks(combined))
        {
            var page = await GetAsync<SearchPageDto>($"api/search/{type}?q=match&pageSize=3");
            Assert.Equal(Math.Min(count, 3), block.Items.Count);
            Assert.Equal(hasMore, block.HasMore);
            Assert.Equal(block.Items, page.Items);
            Assert.Equal(block.HasMore, page.HasMore);
        }
    }

    [Theory]
    [InlineData("departments")]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task SearchPage_ShouldOrderByNameAndId_WithoutGapsOrOverlaps(string type)
    {
        var expected = new List<SearchItemDto>();
        for (var i = 7; i > 0; i--)
        {
            var duplicateName = i > 3 ? "Match B" : "Match A";
            var name = string.Equals(type, "locations", StringComparison.Ordinal) ? $"Match {i:D2}" : duplicateName;
            var id = Guid.Parse($"00000000-0000-0000-0000-{i:D12}");
            await SeedAsync(type, name, id: id);
            expected.Add(new SearchItemDto(id, name));
        }
        var combined = await GetAsync<SearchResultDto>("api/search?q=match&limitPerType=3");
        var first = await GetAsync<SearchPageDto>($"api/search/{type}?q=match&pageSize=3");
        var second = await GetAsync<SearchPageDto>($"api/search/{type}?q=match&page=2&pageSize=3");
        var third = await GetAsync<SearchPageDto>($"api/search/{type}?q=match&page=3&pageSize=3");
        var block = Blocks(combined).Single(b => string.Equals(b.Type, type, StringComparison.Ordinal)).Page;
        Assert.Equal(block.Items, first.Items);
        Assert.Equal(block.HasMore, first.HasMore);
        Assert.True(first.HasMore);
        Assert.True(second.HasMore);
        Assert.False(third.HasMore);
        Assert.Equal(expected.OrderBy(i => i.Name, StringComparer.Ordinal).ThenBy(i => i.Id),
            first.Items.Concat(second.Items).Concat(third.Items));
    }

    [Theory]
    [InlineData("departments", 20)]
    [InlineData("locations", 20)]
    [InlineData("positions", 20)]
    [InlineData("departments", int.MaxValue)]
    [InlineData("locations", int.MaxValue)]
    [InlineData("positions", int.MaxValue)]
    public async Task SearchPage_ShouldReturnEmptyBeyondEnd_WithoutOffsetOverflow(string type, int page)
    {
        await SeedAsync(type, "Match");
        var result = await GetAsync<SearchPageDto>($"api/search/{type}?q=match&page={page}&pageSize=100");
        Assert.Empty(result.Items);
        Assert.False(result.HasMore);
    }

    [Theory]
    [InlineData("departments")]
    [InlineData("locations")]
    [InlineData("positions")]
    public async Task SearchPage_ShouldUseDefaults_AndQueryOnlySelectedTable(string type)
    {
        for (var i = 0; i < 21; i++)
            await SeedAsync(type, $"Match {i:D3}");
        await using var scope = _factory.Services.CreateAsyncScope();
        using var connection = await scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>()
            .OpenConnectionAsync(CancellationToken.None);
        var otherTables = EntityTypes.Where(t => !string.Equals(t, type, StringComparison.Ordinal)).ToArray();
        // Make accidental queries of either other entity fail at the database boundary.
        foreach (var table in otherTables)
            await connection.ExecuteAsync($"ALTER TABLE {table} RENAME TO unavailable_{table}");
        try
        {
            var result = await GetAsync<SearchPageDto>($"api/search/{type}?q=match");
            Assert.Equal(20, result.Items.Count);
            Assert.Equal("Match 000", result.Items[0].Name);
            Assert.True(result.HasMore);
        }
        finally
        {
            foreach (var table in otherTables)
                await connection.ExecuteAsync($"ALTER TABLE unavailable_{table} RENAME TO {table}");
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("q=")]
    [InlineData("q=%20%20")]
    [InlineData("q=x")]
    [InlineData("q=%20x%20")]
    public async Task Search_ShouldRejectInvalidQuery_OnBothEndpoints(string query)
    {
        foreach (var path in new[] { "api/search", "api/search/departments", "api/search/locations", "api/search/positions" })
            await AssertErrorAsync($"{path}?{query}", HttpStatusCode.BadRequest, "search.query.invalid");
    }

    [Theory]
    [InlineData("limitPerType=0", "search.limit.invalid")]
    [InlineData("limitPerType=-1", "search.limit.invalid")]
    [InlineData("limitPerType=101", "search.limit.invalid")]
    [InlineData("limitPerType=", "invalid.model.state")]
    [InlineData("limitPerType=1.5", "invalid.model.state")]
    [InlineData("limitPerType=no", "invalid.model.state")]
    [InlineData("limitPerType=2147483648", "invalid.model.state")]
    public async Task Search_ShouldRejectInvalidLimit(string parameter, string code) =>
        await AssertErrorAsync($"api/search?q=ab&{parameter}", HttpStatusCode.BadRequest, code);

    [Theory]
    [InlineData("page=0", "search.page.invalid")]
    [InlineData("page=-1", "search.page.invalid")]
    [InlineData("page=", "invalid.model.state")]
    [InlineData("page=1.5", "invalid.model.state")]
    [InlineData("page=no", "invalid.model.state")]
    [InlineData("page=2147483648", "invalid.model.state")]
    [InlineData("pageSize=0", "search.page.size.invalid")]
    [InlineData("pageSize=-1", "search.page.size.invalid")]
    [InlineData("pageSize=101", "search.page.size.invalid")]
    [InlineData("pageSize=", "invalid.model.state")]
    [InlineData("pageSize=1.5", "invalid.model.state")]
    [InlineData("pageSize=no", "invalid.model.state")]
    [InlineData("pageSize=2147483648", "invalid.model.state")]
    public async Task SearchPage_ShouldRejectInvalidPagination(string parameter, string code) =>
        await AssertErrorAsync($"api/search/positions?q=ab&{parameter}", HttpStatusCode.BadRequest, code);

    [Theory]
    [InlineData("unknown")]
    [InlineData("Departments")]
    public async Task SearchPage_ShouldReturnNotFoundForUnknownType(string type) =>
        await AssertErrorAsync($"api/search/{type}?q=ab", HttpStatusCode.NotFound, "search.entity.type.not.found");

    [Fact]
    public async Task SearchIndexes_ShouldBePartialGinIndexes_ForAllTypes()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        using var connection = await scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>()
            .OpenConnectionAsync(CancellationToken.None);
        foreach (var type in EntityTypes)
        {
            var definition = await connection.QuerySingleAsync<string>(
                "SELECT indexdef FROM pg_indexes WHERE schemaname = 'public' AND indexname = @name",
                new { name = $"ix_{type}_active_name_trgm" });
            Assert.Contains("USING gin (name gin_trgm_ops)", definition, StringComparison.Ordinal);
            Assert.Contains("WHERE (deleted_at IS NULL)", definition, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task OpenApi_ShouldDescribeSearchContractsAndParameterRules()
    {
        using var response = await _client.GetAsync("openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var paths = document.RootElement.GetProperty("paths");
        foreach (var path in new[] { "/api/search", "/api/search/{entityType}" })
        {
            var operation = paths.GetProperty(path).GetProperty("get");
            var parameters = operation.GetProperty("parameters").EnumerateArray()
                .ToDictionary(p => p.GetProperty("name").GetString()!, StringComparer.Ordinal);
            Assert.True(parameters["q"].GetProperty("required").GetBoolean());
            Assert.Equal(2, parameters["q"].GetProperty("schema").GetProperty("minLength").GetInt32());
            var sizeName = path.EndsWith('}') ? "pageSize" : "limitPerType";
            var size = parameters[sizeName].GetProperty("schema");
            Assert.Equal(20, size.GetProperty("default").GetInt32());
            Assert.Equal(1, size.GetProperty("minimum").GetInt32());
            Assert.Equal(100, size.GetProperty("maximum").GetInt32());
            var responses = operation.GetProperty("responses");
            Assert.True(responses.TryGetProperty("200", out _));
            Assert.True(responses.TryGetProperty("400", out _));
            Assert.True(responses.TryGetProperty("500", out _));
            var example = responses.GetProperty("200").GetProperty("content").GetProperty("application/json")
                .GetProperty("example").GetProperty("result");
            if (parameters.TryGetValue("entityType", out var entityType))
            {
                Assert.True(responses.TryGetProperty("404", out _));
                Assert.Equal(EntityTypes, entityType.GetProperty("schema").GetProperty("enum").EnumerateArray().Select(v => v.GetString()));
                Assert.Equal(1, parameters["page"].GetProperty("schema").GetProperty("default").GetInt32());
                Assert.True(example.TryGetProperty("items", out _));
            }
            else
            {
                Assert.All(EntityTypes, type => Assert.True(example.TryGetProperty(type, out _)));
            }
        }
    }

    private async Task<T> GetAsync<T>(string url)
    {
        using var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        return envelope.Result;
    }

    private async Task AssertErrorAsync(string url, HttpStatusCode status, string code)
    {
        using var response = await _client.GetAsync(url);
        Assert.Equal(status, response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Null(envelope.Result);
        Assert.Contains(envelope.Error!.Messages, e => string.Equals(e.Code, code, StringComparison.Ordinal));
    }

    private async Task SeedAsync(string type, string name, bool deleted = false, Guid? id = null)
    {
        var sql = type switch
        {
            "departments" => "INSERT INTO departments (id, name, slug, path, depth, deleted_at) VALUES (@id, @name, @slug, @slug::ltree, 0, @deletedAt)",
            "locations" => "INSERT INTO locations (id, name, country, city, street, house_number, deleted_at) VALUES (@id, @name, 'Russia', 'Moscow', 'Street', '1', @deletedAt)",
            "positions" => "INSERT INTO positions (id, name, deleted_at) VALUES (@id, @name, @deletedAt)",
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        await using var scope = _factory.Services.CreateAsyncScope();
        using var connection = await scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>()
            .OpenConnectionAsync(CancellationToken.None);
        var rowId = id ?? Guid.NewGuid();
        await connection.ExecuteAsync(sql, new
        {
            id = rowId, name, slug = "seed" + rowId.ToString("N"),
            deletedAt = deleted ? (DateTime?)DateTime.UtcNow : null,
        });
    }

    private static (string Type, SearchPageDto Page)[] Blocks(SearchResultDto result) =>
        [("departments", result.Departments), ("locations", result.Locations), ("positions", result.Positions)];

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
