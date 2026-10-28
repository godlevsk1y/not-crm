using System.Net.Http.Json;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;
using Shared.Kernel.Results;

namespace DirectoryService.IntegrationTests.Positions.Queries;

public class GetPositionListTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;

    public GetPositionListTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPositionList_ShouldUseDefaults_AndExcludeDeletedPositions()
    {
        var positions = await CreatePositionsAsync();

        var result = await GetListAsync("api/positions");

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(8, result.TotalCount);
        Assert.Equal(positions.OrderBy(p => p.Name, StringComparer.Ordinal), result.Results);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    public async Task GetPositionList_ShouldFilterByActiveStatusBeforePagination(bool isActive, int page)
    {
        var positions = await CreatePositionsAsync();
        var result = await GetListAsync(
            $"api/positions?isActive={isActive}&search=engineer&page={page}&pageSize=5");

        Assert.Equal(isActive ? 7 : 1, result.TotalCount);
        if (isActive)
        {
            var expected = positions.Where(p => p.Name.Contains("Engineer", StringComparison.Ordinal))
                .OrderBy(p => p.Name, StringComparer.Ordinal).Skip((page - 1) * 5).Take(5);
            Assert.Equal(expected, result.Results);
        }
        else if (page == 1)
        {
            Assert.Equal("Engineer I", Assert.Single(result.Results).Name);
        }
        else
        {
            Assert.Empty(result.Results);
        }
    }

    [Theory]
    [InlineData("name", "asc")]
    [InlineData("name", "desc")]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    public async Task GetPositionList_ShouldCombineSearchSortingAndPagination(string sortBy, string sortDirection)
    {
        var positions = (await CreatePositionsAsync()).Where(p => p.Name.Contains("Engineer", StringComparison.Ordinal));
        var ordered = (sortBy, sortDirection) switch
        {
            ("name", "asc") => positions.OrderBy(p => p.Name, StringComparer.Ordinal),
            ("name", "desc") => positions.OrderByDescending(p => p.Name, StringComparer.Ordinal),
            ("createdAt", "asc") => positions.OrderBy(p => p.CreatedAt),
            _ => positions.OrderByDescending(p => p.CreatedAt),
        };
        var expected = ordered.ToArray();

        var first = await GetListAsync($"api/positions?search=eNgInEeR&sortBy={sortBy}&sortDirection={sortDirection}&page=1&pageSize=5");
        var second = await GetListAsync($"api/positions?search=eNgInEeR&sortBy={sortBy}&sortDirection={sortDirection}&page=2&pageSize=5");

        Assert.Equal(7, first.TotalCount);
        Assert.Equal(7, second.TotalCount);
        Assert.Equal(1, first.Page);
        Assert.Equal(2, second.Page);
        Assert.Equal(5, first.PageSize);
        Assert.Equal(5, second.PageSize);
        Assert.Equal(expected.Take(5), first.Results);
        Assert.Equal(expected.Skip(5), second.Results);
    }

    [Theory]
    [InlineData("name", "asc")]
    [InlineData("name", "desc")]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    public async Task GetPositionList_ShouldOrderTiesByIdAcrossPages(string sortBy, string sortDirection)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ids = new List<Guid>();
        for (var i = 7; i > 0; i--)
        {
            var position = Position.Create(PositionName.Create("Engineer").Value);
            var id = Guid.Parse($"00000000-0000-0000-0000-{i:D12}");
            context.Entry(position).Property(p => p.Id).CurrentValue = new PositionId(id);
            context.Entry(position).Property(p => p.CreatedAt).CurrentValue = createdAt;
            context.Positions.Add(position);
            ids.Add(id);
        }
        await context.SaveChangesAsync();

        var first = await GetListAsync($"api/positions?sortBy={sortBy}&sortDirection={sortDirection}&page=1&pageSize=5");
        var second = await GetListAsync($"api/positions?sortBy={sortBy}&sortDirection={sortDirection}&page=2&pageSize=5");

        Assert.Equal(ids.Order(), first.Results.Concat(second.Results).Select(p => p.Id));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    [InlineData(int.MaxValue)]
    public async Task GetPositionList_ShouldPreserveFilteredTotalCount_WhenPageIsOutOfRange(int page)
    {
        await CreatePositionsAsync();

        var result = await GetListAsync($"api/positions?search=engineer&page={page}&pageSize=5");

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData("api/positions", false)]
    [InlineData("api/positions?search=missing", true)]
    [InlineData("api/positions?search=missing&page=20", true)]
    public async Task GetPositionList_ShouldReturnEmptyList_WhenNothingMatches(string requestPath, bool seed)
    {
        if (seed)
            await CreatePositionsAsync();

        var result = await GetListAsync(requestPath);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData("Engineer%20_", 7)]
    [InlineData("%25", 8)]
    [InlineData("", 8)]
    public async Task GetPositionList_ShouldSupportILikeWildcardsAndEmptySearch(string search, int expectedCount)
    {
        await CreatePositionsAsync();

        var result = await GetListAsync($"api/positions?search={search}");

        Assert.Equal(expectedCount, result.TotalCount);
        Assert.Equal(expectedCount, result.Results.Count());
    }

    [Theory]
    [InlineData("page=0", "positions.page.invalid", "Page")]
    [InlineData("page=-1", "positions.page.invalid", "Page")]
    [InlineData("pageSize=4", "positions.page.size.invalid", "PageSize")]
    [InlineData("pageSize=101", "positions.page.size.invalid", "PageSize")]
    [InlineData("sortBy=invalid", "positions.sort.by.invalid", "SortBy")]
    [InlineData("sortBy=Name", "positions.sort.by.invalid", "SortBy")]
    [InlineData("sortDirection=invalid", "positions.sort.direction.invalid", "SortDirection")]
    [InlineData("sortDirection=ASC", "positions.sort.direction.invalid", "SortDirection")]
    public async Task GetPositionList_ShouldReturnValidationErrors(string query, string code, string field)
    {
        await AssertErrorAsync(query, code, field);
    }

    [Fact]
    public async Task GetPositionList_ShouldRejectSearchLongerThan100Characters()
    {
        await AssertErrorAsync($"search={new string('a', 101)}", "positions.search.too.long", "Search");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    public async Task GetPositionList_ShouldAcceptBoundaryValues(int pageSize)
    {
        var result = await GetListAsync($"api/positions?search={new string('a', 100)}&pageSize={pageSize}");

        Assert.Equal(pageSize, result.PageSize);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData("page=invalid")]
    [InlineData("pageSize=invalid")]
    [InlineData("isActive=")]
    [InlineData("isActive=null")]
    [InlineData("isActive=invalid")]
    [InlineData("isActive=0")]
    [InlineData("isActive=1")]
    public async Task GetPositionList_ShouldReturnStandardError_WhenModelBindingFails(string query)
    {
        using var response = await _client.GetAsync($"api/positions?{query}");

        Assert.Equal(400, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("invalid.model.state", envelope.Error!.Messages[0].Code);
    }

    private async Task AssertErrorAsync(string query, string code, string field)
    {
        using var response = await _client.GetAsync($"api/positions?{query}");

        Assert.Equal(400, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.NotNull(envelope.Error);
        var error = Assert.Single(envelope.Error.Messages);
        Assert.Equal(code, error.Code);
        Assert.Equal(field, error.InvalidField);
    }

    private async Task<PagedResult<PositionListItemDto>> GetListAsync(string url)
    {
        using var response = await _client.GetAsync(url);
        Assert.Equal(200, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<PagedResult<PositionListItemDto>>>();
        Assert.NotNull(envelope);
        Assert.False(envelope.IsError);
        Assert.NotNull(envelope.Result);
        return envelope.Result;
    }

    private async Task<List<PositionListItemDto>> CreatePositionsAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var result = new List<PositionListItemDto>();
        for (var i = 0; i < 9; i++)
        {
            var name = i == 7 ? "Manager" : $"Engineer {(char)('A' + i)}";
            var position = Position.Create(PositionName.Create(name).Value);
            var createdAt = new DateTime(2026, 1, 9 - i, 0, 0, 0, DateTimeKind.Utc);
            context.Entry(position).Property(p => p.CreatedAt).CurrentValue = createdAt;
            if (i == 8)
                position.SoftDelete();
            else
                result.Add(new PositionListItemDto(position.Id, name, createdAt));
            context.Positions.Add(position);
        }
        await context.SaveChangesAsync();
        return result;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
