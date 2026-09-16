using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class DepartmentPositionsRepository : IDepartmentPositionsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public DepartmentPositionsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken)
    {
        return await _context.DepartmentPositions.AnyAsync(
            departmentPosition =>
                departmentPosition.DepartmentId == departmentId &&
                departmentPosition.PositionId == positionId,
            cancellationToken);
    }

    public async Task AddAsync(
        DepartmentPosition departmentPosition,
        CancellationToken cancellationToken)
    {
        await _context.DepartmentPositions.AddAsync(departmentPosition, cancellationToken);
    }

    public async Task<DepartmentPosition?> GetAsync(
        DepartmentId departmentId,
        PositionId positionId,
        CancellationToken cancellationToken)
    {
        return await _context.DepartmentPositions.FirstOrDefaultAsync(
            departmentPosition =>
                departmentPosition.DepartmentId == departmentId &&
                departmentPosition.PositionId == positionId,
            cancellationToken);
    }

    public void Remove(DepartmentPosition departmentPosition)
    {
        _context.DepartmentPositions.Remove(departmentPosition);
    }

    public async Task<int> RemoveAllByPositionIdAsync(PositionId positionId, CancellationToken cancellationToken)
    {
        return await  _context.DepartmentPositions
            .Where(dp => dp.PositionId == positionId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
