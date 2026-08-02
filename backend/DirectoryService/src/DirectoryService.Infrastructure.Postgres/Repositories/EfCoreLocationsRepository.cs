using DirectoryService.Core.Features.Locations;
using DirectoryService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Infrastructure.Postgres.Repositories;

public class EfCoreLocationsRepository : ILocationsRepository
{
    private readonly DirectoryServiceDbContext _context;

    public EfCoreLocationsRepository(DirectoryServiceDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> AddAsync(Location location, CancellationToken cancellationToken)
    {
        await _context.Locations.AddAsync(location, cancellationToken);
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return location.Id;
    }

    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Location?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Locations.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Location>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return [];
        
        return await _context.Locations.Where(l => ids.Contains(l.Id)).ToListAsync(cancellationToken);
    }

    public async Task<Location?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await _context.Locations.FirstOrDefaultAsync(l => l.Name.Value == name, cancellationToken);
    }
}