using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Features.Locations;

public interface ILocationsRepository
{
    Task<Guid> AddAsync(Location location, CancellationToken cancellationToken);
    
    Task SaveAsync(CancellationToken cancellationToken);
    
    Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    
    Task<Location?> GetByNameAsync(string name, CancellationToken cancellationToken);
}