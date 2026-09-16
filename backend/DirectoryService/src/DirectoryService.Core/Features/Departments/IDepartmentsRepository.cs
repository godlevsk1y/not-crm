using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Departments;

public interface IDepartmentsRepository
{
    Task<Guid> AddAsync(Department department, CancellationToken cancellationToken);
    
    Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken);
    
    Task<Department?> GetByIdWithParentAsync(DepartmentId id, CancellationToken cancellationToken);

    void Delete(Department department);
    
    Task<bool> HasActiveChildrenAsync(DepartmentId parentId, CancellationToken cancellationToken);
}
