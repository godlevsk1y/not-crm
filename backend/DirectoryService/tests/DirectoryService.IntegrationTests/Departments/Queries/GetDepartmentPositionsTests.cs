using System.Net.Http.Json;
using System.Text.Json;
using DirectoryService.Contracts.Positions.QueryContracts;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using Microsoft.Extensions.DependencyInjection;
using Shared.Framework.Results;
using Shared.Kernel.Results;

namespace DirectoryService.IntegrationTests.Departments.Queries;

public class GetDepartmentPositionsTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;
    private readonly HttpClient _client;
    private Guid _departmentId = Guid.NewGuid();

    public GetDepartmentPositionsTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDepartmentPositions_ShouldUseDefaults_AndReturnOnlyDirectActivePositions()
    {
        var positions = await CreatePositionsAsync();

        var result = await GetListAsync($"api/departments/{_departmentId}/positions");

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(positions.OrderBy(p => p.Name, StringComparer.Ordinal), result.Results);
    }

    [Theory]
    [InlineData("name", "asc")]
    [InlineData("name", "desc")]
    [InlineData("createdAt", "asc")]
    [InlineData("createdAt", "desc")]
    public async Task GetDepartmentPositions_ShouldSortAndPaginate(string sortBy, string sortDirection)
    {
        var positions = await CreatePositionsAsync();
        var ordered = (sortBy, sortDirection) switch
        {
            ("name", "asc") => positions.OrderBy(p => p.Name, StringComparer.Ordinal),
            ("name", "desc") => positions.OrderByDescending(p => p.Name, StringComparer.Ordinal),
            ("createdAt", "asc") => positions.OrderBy(p => p.CreatedAt),
            _ => positions.OrderByDescending(p => p.CreatedAt),
        };
        var expected = ordered.ToArray();

        var first = await GetListAsync($"api/departments/{_departmentId}/positions?sortBy={sortBy}&sortDirection={sortDirection}&page=1&pageSize=5");
        var second = await GetListAsync($"api/departments/{_departmentId}/positions?sortBy={sortBy}&sortDirection={sortDirection}&page=2&pageSize=5");

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
    public async Task GetDepartmentPositions_ShouldOrderTiesByIdAcrossPages(string sortBy, string sortDirection)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var department = CreateDepartment(context, "Product", "product");
        _departmentId = department.Id;
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ids = new List<Guid>();
        for (var i = 7; i > 0; i--)
        {
            var position = Position.Create(PositionName.Create("Engineer").Value);
            var id = Guid.Parse($"00000000-0000-0000-0000-{i:D12}");
            context.Entry(position).Property(p => p.Id).CurrentValue = new PositionId(id);
            context.Entry(position).Property(p => p.CreatedAt).CurrentValue = createdAt;
            context.Positions.Add(position);
            context.DepartmentPositions.Add(new DepartmentPosition(department.Id, position.Id));
            ids.Add(id);
        }
        await context.SaveChangesAsync();

        var first = await GetListAsync($"api/departments/{_departmentId}/positions?sortBy={sortBy}&sortDirection={sortDirection}&page=1&pageSize=5");
        var second = await GetListAsync($"api/departments/{_departmentId}/positions?sortBy={sortBy}&sortDirection={sortDirection}&page=2&pageSize=5");

        Assert.Equal(ids.Order(), first.Results.Concat(second.Results).Select(p => p.Id));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(20)]
    [InlineData(int.MaxValue)]
    public async Task GetDepartmentPositions_ShouldPreserveTotalCount_WhenPageIsOutOfRange(int page)
    {
        await CreatePositionsAsync();

        var result = await GetListAsync($"api/departments/{_departmentId}/positions?page={page}&pageSize=5");

        Assert.Equal(7, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public async Task GetDepartmentPositions_ShouldReturnEmptyPage_WhenNoActivePositionsExist(bool deletedOnly, int page)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var department = CreateDepartment(context, "Product", "product");
        _departmentId = department.Id;
        if (deletedOnly)
        {
            var position = Position.Create(PositionName.Create("Engineer").Value);
            position.SoftDelete();
            context.Positions.Add(position);
            context.DepartmentPositions.Add(new DepartmentPosition(department.Id, position.Id));
        }
        await context.SaveChangesAsync();

        var result = await GetListAsync($"api/departments/{_departmentId}/positions?page={page}&pageSize=5");

        Assert.Equal(0, result.TotalCount);
        Assert.Equal(page, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetDepartmentPositions_ShouldReturnNotFound_WhenDepartmentIsMissingOrDeleted(bool deleted)
    {
        if (deleted)
        {
            await CreatePositionsAsync();
            await using var scope = _factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            var department = await context.Departments.FindAsync(new DepartmentId(_departmentId));
            Assert.NotNull(department);
            department.SoftDelete();
            await context.SaveChangesAsync();
        }

        using var response = await _client.GetAsync($"api/departments/{_departmentId}/positions");

        Assert.Equal(404, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("department.not.found", Assert.Single(envelope.Error!.Messages).Code);
    }

    [Theory]
    [InlineData("page=0", "positions.page.invalid", "Page")]
    [InlineData("page=-1", "positions.page.invalid", "Page")]
    [InlineData("pageSize=4", "positions.page.size.invalid", "PageSize")]
    [InlineData("pageSize=101", "positions.page.size.invalid", "PageSize")]
    [InlineData("sortBy=invalid", "positions.sort.by.invalid", "SortBy")]
    [InlineData("sortBy=Name", "positions.sort.by.invalid", "SortBy")]
    [InlineData("sortBy=name%3BDROP%20TABLE%20positions", "positions.sort.by.invalid", "SortBy")]
    [InlineData("sortDirection=invalid", "positions.sort.direction.invalid", "SortDirection")]
    [InlineData("sortDirection=ASC", "positions.sort.direction.invalid", "SortDirection")]
    public async Task GetDepartmentPositions_ShouldReturnValidationErrors(string query, string code, string field)
    {
        await AssertErrorAsync(query, code, field);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(100)]
    public async Task GetDepartmentPositions_ShouldAcceptBoundaryValues(int pageSize)
    {
        await CreatePositionsAsync();

        var result = await GetListAsync($"api/departments/{_departmentId}/positions?pageSize={pageSize}");

        Assert.Equal(pageSize, result.PageSize);
        Assert.Equal(7, result.TotalCount);
        Assert.Equal(Math.Min(pageSize, 7), result.Results.Count());
    }

    [Theory]
    [InlineData("page=invalid")]
    [InlineData("pageSize=invalid")]
    [InlineData("page=")]
    [InlineData("pageSize=")]
    [InlineData("page=2147483648")]
    [InlineData("sortBy=")]
    [InlineData("sortDirection=")]
    public async Task GetDepartmentPositions_ShouldReturnStandardError_WhenModelBindingFails(string query)
    {
        using var response = await _client.GetAsync($"api/departments/{_departmentId}/positions?{query}");

        Assert.Equal(400, (int)response.StatusCode);
        var envelope = await response.Content.ReadFromJsonAsync<Envelope>();
        Assert.NotNull(envelope);
        Assert.True(envelope.IsError);
        Assert.Equal("invalid.model.state", envelope.Error!.Messages[0].Code);
    }

    [Fact]
    public async Task GetDepartmentPositions_ShouldDocumentQueryRulesAndResponses()
    {
        using var response = await _client.GetAsync("openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/api/departments/{id}/positions").GetProperty("get");
        var parameters = operation.GetProperty("parameters").EnumerateArray()
            .Where(p => string.Equals(p.GetProperty("in").GetString(), "query", StringComparison.Ordinal))
            .ToDictionary(p => p.GetProperty("name").GetString()!, p => p.GetProperty("schema"), StringComparer.OrdinalIgnoreCase);

        Assert.Equal(4, parameters.Count);
        Assert.Equal("name", parameters["sortBy"].GetProperty("default").GetString());
        Assert.Collection(parameters["sortBy"].GetProperty("enum").EnumerateArray(),
            v => Assert.Equal("name", v.GetString()),
            v => Assert.Equal("createdAt", v.GetString()));
        Assert.Equal("asc", parameters["sortDirection"].GetProperty("default").GetString());
        Assert.Collection(parameters["sortDirection"].GetProperty("enum").EnumerateArray(),
            v => Assert.Equal("asc", v.GetString()),
            v => Assert.Equal("desc", v.GetString()));
        Assert.Equal(1, parameters["page"].GetProperty("minimum").GetInt32());
        Assert.Equal(1, parameters["page"].GetProperty("default").GetInt32());
        Assert.Equal(5, parameters["pageSize"].GetProperty("minimum").GetInt32());
        Assert.Equal(100, parameters["pageSize"].GetProperty("maximum").GetInt32());
        Assert.Equal(20, parameters["pageSize"].GetProperty("default").GetInt32());
        var responses = operation.GetProperty("responses");
        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("404", out _));
        Assert.Contains("positions.sort.by.invalid", responses.GetProperty("400").GetRawText(), StringComparison.Ordinal);
        Assert.Contains("department.not.found", responses.GetProperty("404").GetRawText(), StringComparison.Ordinal);
    }

    private async Task AssertErrorAsync(string query, string code, string field)
    {
        using var response = await _client.GetAsync($"api/departments/{_departmentId}/positions?{query}");

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
        var parent = CreateDepartment(context, "Parent", "parent");
        var department = CreateDepartment(context, "Product", "product", parent);
        _departmentId = department.Id;
        var child = CreateDepartment(context, "Engineering", "engineering", department);
        var other = CreateDepartment(context, "Business", "business");
        var result = new List<PositionListItemDto>();
        for (var i = 0; i < 12; i++)
        {
            var name = $"Engineer {(char)('A' + i)}";
            var position = Position.Create(PositionName.Create(name).Value);
            var createdAt = new DateTime(2026, 1, 12 - i, 0, 0, 0, DateTimeKind.Utc);
            context.Entry(position).Property(p => p.CreatedAt).CurrentValue = createdAt;
            context.Positions.Add(position);
            if (i < 8)
                context.DepartmentPositions.Add(new DepartmentPosition(department.Id, position.Id));
            if (i == 7)
                position.SoftDelete();
            if (i < 7)
                result.Add(new PositionListItemDto(position.Id, name, createdAt));
            if (i is 0 or 8)
                context.DepartmentPositions.Add(new DepartmentPosition(child.Id, position.Id));
            if (i is 0 or 9)
                context.DepartmentPositions.Add(new DepartmentPosition(other.Id, position.Id));
            if (i == 10)
                context.DepartmentPositions.Add(new DepartmentPosition(parent.Id, position.Id));
        }
        await context.SaveChangesAsync();
        return result;
    }

    private static Department CreateDepartment(DirectoryServiceDbContext context, string name, string slug, Department? parent = null)
    {
        var department = Department.Create(DepartmentName.Create(name).Value, Slug.Create(slug).Value, parent);
        context.Departments.Add(department);
        return department;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
