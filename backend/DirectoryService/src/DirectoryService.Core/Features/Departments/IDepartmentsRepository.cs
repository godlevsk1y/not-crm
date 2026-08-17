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

    void Delete(Department department);
    
    Task<bool> HasDepartmentLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken);
    
    Task AddLocationAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken);

    Task<bool> HasDepartmentPositionAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken);

    Task AddPositionAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken);
    
    void RemoveLocation(DepartmentLocation departmentLocation);
    
    Task<DepartmentLocation?> GetDepartmentLocation(DepartmentId departmentId, LocationId locationId, CancellationToken cancellationToken);

    void RemovePosition(DepartmentPosition departmentPosition);

    Task<DepartmentPosition?> GetDepartmentPosition(DepartmentId departmentId, PositionId positionId, CancellationToken cancellationToken);
}
