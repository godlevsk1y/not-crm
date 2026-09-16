using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Departments;

public interface IDepartmentLocationsRepository
{
    Task AddRangeAsync(
        IEnumerable<DepartmentLocation> departmentLocations,
        CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken);

    Task AddAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken);

    Task<DepartmentLocation?> GetAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken);

    void Remove(DepartmentLocation departmentLocation);
}
