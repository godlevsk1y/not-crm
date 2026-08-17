using DirectoryService.Core.Features.Locations;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class LocationsRepository : ILocationsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public LocationsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> AddAsync(Location location, CancellationToken cancellationToken)
    {
        await _context.Locations.AddAsync(location, cancellationToken);
        
        return location.Id;
    }

    public async Task<Location?> GetByIdAsync(LocationId id, CancellationToken cancellationToken)
    {
        return await _context.Locations.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> GetByIdsAsync(IReadOnlyCollection<LocationId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];
        
        return await _context.Locations.Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);
    }

    public async Task<Location?> GetByNameAsync(LocationName name, CancellationToken cancellationToken)
    {
        return await _context.Locations.FirstOrDefaultAsync(l => l.Name == name, cancellationToken);
    }

    public void Delete(Location location)
    {
        _context.Locations.Remove(location);
    }
}
