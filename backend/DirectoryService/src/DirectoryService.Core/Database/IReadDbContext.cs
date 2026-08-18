using DirectoryService.Domain.Models;

namespace DirectoryService.Core.Database;

public interface IReadDbContext
{
    IQueryable<Location> LocationsRead { get; }
}