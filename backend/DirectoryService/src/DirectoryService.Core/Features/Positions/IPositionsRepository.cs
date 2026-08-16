using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Positions;

public interface IPositionsRepository
{
    Task<Guid> AddAsync(Position position, CancellationToken cancellationToken);

    Task<Position?> GetByIdAsync(PositionId id, CancellationToken cancellationToken);
}
