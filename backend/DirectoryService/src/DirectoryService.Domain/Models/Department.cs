using CSharpFunctionalExtensions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models.Errors;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using Path = DirectoryService.Domain.ValueObjects.Path;

namespace DirectoryService.Domain.Models;

public sealed class Department
{
    public DepartmentId Id { get; private set; } = null!;
    
    public DepartmentName Name { get; private set; } = null!;
    
    public Slug Slug { get; private set; } = null!;
    
    public Path Path { get; private set; } = null!;
    
    public DepartmentId? ParentId { get; private set; }
    
    public Department? Parent { get ; private set; }
    
    public int Depth { get; private set;  }
    
    public DateTime CreatedAt { get; private set; }
    
    public DateTime UpdatedAt { get; private set; }
    
    public DateTime? DeletedAt { get; private set; }
    
    
    private Department() { } // EF Core
    
    private Department(DepartmentName name, Slug slug, Department? parent = null)
    {
        Id = new DepartmentId(Guid.NewGuid());
        Name = name;
        Slug = slug;
        Parent = parent;
        ParentId = parent?.Id;
        Depth = CalculateDepth();
        
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        Path = CalculatePath();
    }

    public static Department Create(DepartmentName name, Slug slug, Department? parent = null)
    {
        return new Department(name, slug, parent);
    }
    
    public void Rename(DepartmentName name)
    {
        Name = name;
        
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void ChangeSlug(Slug slug)
    {
        Slug = slug;
        
        UpdatedAt = DateTime.UtcNow;
        
        Path = CalculatePath();
    }
    
    public UnitResult<Error> SetParent(Department? parent)
    {
        if (parent is not null && parent.Id == Id)
        {
            return ModelErrors.Department.ParentToItself();
        }
        
        Parent = parent;
        ParentId = Parent?.Id;

        Path = CalculatePath();
        Depth = CalculateDepth();
        
        UpdatedAt = DateTime.UtcNow;
        
        return UnitResult.Success<Error>();
    }
    
    public void SoftDelete() => DeletedAt = DateTime.UtcNow;
    
    private Path CalculatePath() => Parent is null ? Path.Create(Slug) : Parent.Path.Append(Slug);
    private int CalculateDepth() => Parent is null ? 0 : Parent.Depth + 1;
}
