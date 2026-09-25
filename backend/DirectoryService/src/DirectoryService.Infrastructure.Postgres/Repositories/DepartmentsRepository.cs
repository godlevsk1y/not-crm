using Dapper;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Path = DirectoryService.Domain.ValueObjects.Path;

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

    public async Task<bool> IsCycle(Department department, Department newParent, CancellationToken cancellationToken)
    {
        LTree departmentPath = department.Path.Value;
        
        return await _context.Departments
            .FromSqlInterpolated($"""
                                 SELECT * 
                                 FROM departments
                                 WHERE path <@ {departmentPath}
                                 """)
            .AnyAsync(d => d.Id == newParent.Id, cancellationToken);
    }

    public async Task<int> RecalculatePathsAsync(Path oldPath, Path newPath, CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        
        var parameters = new DynamicParameters();
        
        parameters.Add("@oldPath", oldPath.Value);
        parameters.Add("@newPath", newPath.Value);
        
        const string sql = """
                           UPDATE departments d
                           SET path =
                                   @newPath::ltree ||
                                   subpath(d.path, nlevel(@oldPath::ltree)),
                               depth = nlevel(@newPath::ltree) +
                                       nlevel(d.path) -
                                       nlevel(@oldPath::ltree) - 1
                           WHERE d.path <@ @oldPath::ltree
                           """;

        return await connection.ExecuteAsync(
            sql: sql, 
            param: parameters
        );
    }
}
