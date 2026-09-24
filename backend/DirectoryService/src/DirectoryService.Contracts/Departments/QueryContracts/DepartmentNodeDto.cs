namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentNodeDto
{
    public Guid Id { get; init; }
    
    public string Name { get; init; } = null!;
    
    public string Slug { get; init; } = null!;
    
    public string Path { get; init; } = null!;
    
    public int Depth { get; init; }
    
    public DateTime CreatedAt { get; init; }
    
    public bool HasChildren { get; init; }
    
    public int ChildCount { get; init; }
}