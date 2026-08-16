using DirectoryService.Core.Features.Positions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class PositionsRepository : IPositionsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public PositionsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> AddAsync(Position position, CancellationToken cancellationToken)
    {
        await _context.Positions.AddAsync(position, cancellationToken);

        return position.Id;
    }

    public async Task<Position?> GetByIdAsync(PositionId id, CancellationToken cancellationToken)
    {
        return await _context.Positions.FirstOrDefaultAsync(
            position => position.Id == id,
            cancellationToken);
    }
}
