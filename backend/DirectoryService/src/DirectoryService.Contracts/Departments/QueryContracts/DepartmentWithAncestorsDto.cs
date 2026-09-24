namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentWithAncestorsDto
{
    public Guid Id { get; init; }
    
    public string Name { get; init; } = null!;
    
    public string Slug { get; init; } = null!;
    
    public string Path { get; init; } = null!;
    
    public int Depth { get; init; }
    
    public Guid? ParentId { get; init; }

    public IList<DepartmentAncestorDto> Ancestors { get; init; } = [];
}