using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentLocationsRepository : IDepartmentLocationsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public DepartmentLocationsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IEnumerable<DepartmentLocation> departmentLocations,
        CancellationToken cancellationToken)
    {
        await _context.DepartmentLocations.AddRangeAsync(departmentLocations, cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        return await _context.DepartmentLocations.AnyAsync(
            departmentLocation =>
                departmentLocation.DepartmentId == departmentId &&
                departmentLocation.LocationId == locationId,
            cancellationToken);
    }

    public async Task AddAsync(
        DepartmentLocation departmentLocation,
        CancellationToken cancellationToken)
    {
        await _context.DepartmentLocations.AddAsync(departmentLocation, cancellationToken);
    }

    public async Task<DepartmentLocation?> GetAsync(
        DepartmentId departmentId,
        LocationId locationId,
        CancellationToken cancellationToken)
    {
        return await _context.DepartmentLocations.FirstOrDefaultAsync(
            departmentLocation =>
                departmentLocation.DepartmentId == departmentId &&
                departmentLocation.LocationId == locationId,
            cancellationToken);
    }

    public void Remove(DepartmentLocation departmentLocation)
    {
        _context.DepartmentLocations.Remove(departmentLocation);
    }
}
