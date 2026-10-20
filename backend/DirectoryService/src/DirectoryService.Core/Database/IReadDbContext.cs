using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Database;

public interface IReadDbContext
{
    IQueryable<Department> DepartmentsRead { get; }
    
    IQueryable<Location> LocationsRead { get; }
}