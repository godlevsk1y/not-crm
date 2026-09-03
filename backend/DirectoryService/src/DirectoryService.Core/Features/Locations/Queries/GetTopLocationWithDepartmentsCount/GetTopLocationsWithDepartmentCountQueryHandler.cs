using Dapper;
using DirectoryService.Contracts.Locations;
using DirectoryService.Contracts.Locations.QueryContracts;
using DirectoryService.Core.Database;

namespace DirectoryService.Core.Features.Locations.Queries.GetTopLocationWithDepartmentsCount;

public class GetTopLocationsWithDepartmentCountQueryHandler
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetTopLocationsWithDepartmentCountQueryHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<LocationWithDepartmentCountDto>> Handle(CancellationToken cancellationToken)
    {
        var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        
        const string sql = """
                           SELECT
                               l.id, 
                               l.name, 
                               l.country, 
                               l.region, 
                               l.city, 
                               l.district, 
                               l.street, 
                               l.house_number, 
                               l.postal_code,
                               count(dl.department_id) AS department_count
                           FROM locations l
                           LEFT JOIN department_locations dl ON l.id = dl.location_id
                           GROUP BY l.id
ORDER BY department_count DESC, l.name ASC
                           LIMIT 5
                           """;

        var locationsWithDepartmentCount = await connection
            .QueryAsync<LocationDto, long, LocationWithDepartmentCountDto>(
                sql,
                map: (dto, count) => new LocationWithDepartmentCountDto(dto, count),
                splitOn: "department_count"
            );
        
        return locationsWithDepartmentCount.ToList();
    }
}