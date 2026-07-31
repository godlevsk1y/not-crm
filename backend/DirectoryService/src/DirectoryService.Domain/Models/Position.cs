using CSharpFunctionalExtensions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models.Errors;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Domain.Models;

public class Position
{
    public PositionId Id { get; private set; } = null!;
    
    public PositionName Name { get; private set; } = null!;
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdatedAt { get; private set; }

    
    private Position() { } // EF core
    
    private Position(PositionName name)
    {
        Id = new PositionId(Guid.NewGuid());
        Name = name;
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static Position Create(PositionName name)
    {
        return new Position(name);
    }
}
