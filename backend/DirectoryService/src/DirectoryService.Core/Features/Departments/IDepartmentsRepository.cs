using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Departments;

public interface IDepartmentsRepository
{
    Task<Guid> AddAsync(
        Department department, 
        IEnumerable<DepartmentLocation> locations, 
        CancellationToken cancellationToken);
    
    Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken);
    
    Task<Department?> GetByIdWithParentAsync(DepartmentId id, CancellationToken cancellationToken);
    
    Task<bool> HasDepartmentLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken);
    
    Task AddLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken);
    
    Task RemoveLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken);
    
    Task<DepartmentLocation?> GetDepartmentLocation(DepartmentId departmentId, LocationId locationId, CancellationToken cancellationToken);
}