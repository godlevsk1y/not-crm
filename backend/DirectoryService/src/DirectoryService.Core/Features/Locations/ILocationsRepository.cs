using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;

namespace DirectoryService.Core.Features.Locations;

public interface ILocationsRepository
{
    Task<Guid> AddAsync(Location location, CancellationToken cancellationToken);
    
    Task<Location?> GetByIdAsync(LocationId id, CancellationToken cancellationToken);
    
    Task<IReadOnlyList<Location>> GetByIdsAsync(IReadOnlyCollection<LocationId> ids, CancellationToken cancellationToken);
    
    Task<Location?> GetByNameAsync(LocationName name, CancellationToken cancellationToken);

    void Delete(Location location);
}
