using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Path = DirectoryService.Domain.ValueObjects.Path;

namespace DirectoryService.Core.Features.Departments;

public interface IDepartmentsRepository
{
    Task<Guid> AddAsync(Department department, CancellationToken cancellationToken);
    
    Task<Department?> GetByIdAsync(DepartmentId id, CancellationToken cancellationToken);
    
    Task<Department?> GetByIdWithParentAsync(DepartmentId id, CancellationToken cancellationToken);

    void Delete(Department department);
    
    Task<bool> HasActiveChildrenAsync(DepartmentId parentId, CancellationToken cancellationToken);
    
    Task<bool> IsCycle(Department department, Department newParent, CancellationToken cancellationToken);
    
    Task<int> RecalculatePathsAsync(Path oldPath, Path newPath, CancellationToken cancellationToken);
}
