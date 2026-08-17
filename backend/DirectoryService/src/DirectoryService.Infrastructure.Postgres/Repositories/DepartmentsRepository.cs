using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentsRepository : IDepartmentsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public DepartmentsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> AddAsync(Department department, IEnumerable<DepartmentLocation> locations, CancellationToken cancellationToken)
    {
        await _context.Departments.AddAsync(department, cancellationToken);
        
        await _context.DepartmentLocations.AddRangeAsync(locations, cancellationToken);
        
        return department.Id;
    }

    public async Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken)
    {
        return await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Department?> GetByIdWithParentAsync(DepartmentId id, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .Include(d => d.Parent)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return department;
    }

    public async Task DeleteAsync(Department department, CancellationToken cancellationToken)
    {
        await _context.Departments
            .Where(existingDepartment => existingDepartment.Id == department.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> HasDepartmentLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken)
    {
        var existing = await _context.DepartmentLocations.FirstOrDefaultAsync(
            dl => dl.DepartmentId == departmentLocation.DepartmentId 
                  && 
                  dl.LocationId == departmentLocation.LocationId, 
            cancellationToken
        );
        
        return existing is not null;
    }

    public async Task AddLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken)
    {
        await _context.DepartmentLocations.AddAsync(departmentLocation, cancellationToken);
    }

    public async Task<bool> HasDepartmentPositionAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken)
    {
        var existing = await _context.DepartmentPositions.FirstOrDefaultAsync(
            dp => dp.DepartmentId == departmentPosition.DepartmentId
                  &&
                  dp.PositionId == departmentPosition.PositionId,
            cancellationToken
        );

        return existing is not null;
    }

    public async Task AddPositionAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken)
    {
        await _context.DepartmentPositions.AddAsync(departmentPosition, cancellationToken);
    }

    public async Task RemoveLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken)
    {
        await _context.DepartmentLocations
            .Where(dl => dl.Id == departmentLocation.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<DepartmentLocation?> GetDepartmentLocation(DepartmentId departmentId, LocationId locationId, CancellationToken cancellationToken)
    {
        return await _context.DepartmentLocations.FirstOrDefaultAsync(
            dl => dl.DepartmentId == departmentId 
                  && 
                  dl.LocationId == locationId, 
            cancellationToken
        );
    }

    public async Task RemovePositionAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken)
    {
        await _context.DepartmentPositions
            .Where(dp => dp.Id == departmentPosition.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<DepartmentPosition?> GetDepartmentPosition(DepartmentId departmentId, PositionId positionId, CancellationToken cancellationToken)
    {
        return await _context.DepartmentPositions.FirstOrDefaultAsync(
            dp => dp.DepartmentId == departmentId
                  &&
                  dp.PositionId == positionId,
            cancellationToken
        );
    }
}
