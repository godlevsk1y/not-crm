using System.Data;
using System.Diagnostics;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Common;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Shared.Errors;
using DirectoryService.Shared.Results;
using FluentValidation;

namespace DirectoryService.Core.Features.Locations.Queries.GetLocationsList;

public class GetLocationListQueryHandler : IQueryHandler<GetLocationListQuery, 
    Result<PagedResult<LocationListItemDto>, Error>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IValidator<GetLocationListQuery> _validator;

    public GetLocationListQueryHandler(
        IDbConnectionFactory connectionFactory,
        IValidator<GetLocationListQuery> validator)
    {
        _connectionFactory = connectionFactory;
        _validator = validator;
    }
    
    public async Task<Result<PagedResult<LocationListItemDto>, Error>> Handle(GetLocationListQuery query, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var parameters = new DynamicParameters();
        
        parameters.Add("search", query.Search, DbType.String);
        parameters.Add("minDepartmentCount", query.MinDepartmentCount ?? 0, DbType.Int32);
        parameters.Add("page", query.Page, DbType.Int32);
        parameters.Add("pageSize", query.PageSize, DbType.Int32);

        var whereClauses = new List<string>();

        if (query.Search is not null)
        {
            whereClauses.Add("l.name ILIKE '%' || @search || '%'");
        }

        whereClauses.Add("COALESCE(dc.department_count, 0) >= @minDepartmentCount");
        
        var whereExpression = $"WHERE {string.Join(" AND ", whereClauses)}";

        var sortBy = query.SortBy switch
        {
            "name" => "lc.name",
            "createdAt" => "lc.created_at",
            "departmentCount" => "lc.department_count",
            _ => throw new UnreachableException()
        };
        
        var sortDirection = query.SortDirection switch
        {
            "asc" => "ASC",
            "desc" => "DESC",
            _ => throw new UnreachableException(),
        };
        
        var sql = $"""
                  WITH locations_counted AS (SELECT
                                                 l.id,
                                                 l.name,
                                                 l.country,
                                                 l.region,
                                                 l.city,
                                                 l.district,
                                                 l.street,
                                                 l.house_number,
                                                 l.postal_code,
                                                 l.created_at,
                                                 COALESCE(dc.department_count, 0) AS department_count
                                             FROM locations l
                                             LEFT JOIN (
                                                 SELECT 
                                                     location_id,
                                                     COUNT(department_id) AS department_count
                                                 FROM department_locations
                                                 GROUP BY location_id
                                             ) dc ON dc.location_id = l.id
                                             {whereExpression}
                  )
                  SELECT lc.id,
                         lc.name,
                         lc.country,
                         lc.region,
                         lc.city,
                         lc.district,
                         lc.street,
                         lc.house_number,
                         lc.postal_code,
                         lc.created_at,
                         lc.department_count,
                         count(lc.id) OVER () AS total_count
                  FROM locations_counted lc
                  ORDER BY {sortBy} {sortDirection}, lc.id
                  LIMIT @pageSize OFFSET (@page - 1) * @pageSize
                  """;

        long? totalCount = null;
        
        var locations = await connection
            .QueryAsync<Guid, string, AddressDto, DateTime, long, long, LocationListItemDto>(
                sql,
                map: (id, name, address, createdAt, departmentCount, count) =>
                {
                    totalCount ??= count;

                    return new LocationListItemDto(
                        Id: id,
                        Name: name,
                        Address: address,
                        CreatedAt: createdAt,
                        DepartmentCount: departmentCount
                    );
                },
                param: parameters,
                splitOn: "name, country, created_at, department_count, total_count"
            );
        
        return new PagedResult<LocationListItemDto>(locations, query.Page, query.PageSize, totalCount ?? 0);
    }
}