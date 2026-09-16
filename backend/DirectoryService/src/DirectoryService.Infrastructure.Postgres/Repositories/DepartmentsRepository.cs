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
    
    public async Task<Guid> AddAsync(Department department, CancellationToken cancellationToken)
    {
        await _context.Departments.AddAsync(department, cancellationToken);

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

    public void Delete(Department department)
    {
        _context.Departments.Remove(department);
    }

    public async Task<bool> HasActiveChildrenAsync(DepartmentId parentId, 
        CancellationToken cancellationToken)
    {
        return await _context.Departments
            .IgnoreQueryFilters()
            .AnyAsync(
                child => 
                    child.ParentId == parentId && 
                    child.DeletedAt == null, 
            cancellationToken);
    }
}
