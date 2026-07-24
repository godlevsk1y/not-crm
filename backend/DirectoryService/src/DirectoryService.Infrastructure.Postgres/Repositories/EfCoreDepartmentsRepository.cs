using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class EfCoreDepartmentsRepository : IDepartmentsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public EfCoreDepartmentsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> AddAsync(Department department, IEnumerable<DepartmentLocation> locations, CancellationToken cancellationToken)
    {
        await _context.Departments.AddAsync(department, cancellationToken);
        
        await _context.DepartmentLocations.AddRangeAsync(locations, cancellationToken);
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return department.Id;
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<Department?> GetByIdWithParentAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .Include(d => d.Parent)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return department;
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
        
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken)
    {
        await _context.DepartmentLocations
            .Where(dl => dl.Id == departmentLocation.Id)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<DepartmentLocation?> GetDepartmentLocation(Guid departmentId, Guid locationId, CancellationToken cancellationToken)
    {
        return await _context.DepartmentLocations.FirstOrDefaultAsync(
            dl => dl.DepartmentId == departmentId 
                  && 
                  dl.LocationId == locationId, 
            cancellationToken
        );
    }
}