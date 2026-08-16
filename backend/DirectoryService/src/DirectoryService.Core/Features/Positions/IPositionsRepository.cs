using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Positions;

public interface IPositionsRepository
{
    Task<Guid> AddAsync(Position position, CancellationToken cancellationToken);
}
