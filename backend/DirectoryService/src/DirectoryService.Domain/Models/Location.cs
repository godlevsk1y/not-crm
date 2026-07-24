using CSharpFunctionalExtensions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models.Errors;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.Models;

public class Location
{
    public LocationId Id { get; private set; } = null!;
    
    public LocationName Name { get; private set; } = null!;
    
    public Address Address { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    
    private Location() { } // EF Core
    
    private Location(LocationId id, LocationName name, Address address)
    {
        Id = id;
        
        Name = name;
        Address = address;
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Location Create(LocationName name, Address address)
    {
        return new Location(new LocationId(Guid.NewGuid()), name, address);
    }
    
    public void Update(LocationName name, Address address)
    {
        Name = name;
        Address = address;
        
        UpdatedAt = DateTime.UtcNow;
    }
}
