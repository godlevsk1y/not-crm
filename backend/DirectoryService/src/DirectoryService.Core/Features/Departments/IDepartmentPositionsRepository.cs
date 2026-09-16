using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Departments;

public interface IDepartmentPositionsRepository
{
    Task<bool> ExistsAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken);

    Task AddAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken);

    Task<DepartmentPosition?> GetAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken);

    void Remove(DepartmentPosition departmentPosition);
    
    Task RemoveAllByPositionIdAsync(PositionId positionId, CancellationToken cancellationToken);
}
