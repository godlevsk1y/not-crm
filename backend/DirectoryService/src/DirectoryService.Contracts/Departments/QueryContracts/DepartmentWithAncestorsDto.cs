using System.ComponentModel;

namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentWithAncestorsDto
{
    [Description("Matching department UUID.")]
    public Guid Id { get; init; }
    
    [Description("Stored department name.")]
    public string Name { get; init; } = null!;
    
    [Description("Stored slug.")]
    public string Slug { get; init; } = null!;
    
    [Description("Dot-separated hierarchy path.")]
    public string Path { get; init; } = null!;
    
    [Description("Depth from root; roots have depth 0.")]
    public int Depth { get; init; }
    
    [Description("Parent UUID; null for a root.")]
    public Guid? ParentId { get; init; }

    [Description("All active ancestors, ordered from root to parent; excludes the matching department.")]
    public IList<DepartmentAncestorDto> Ancestors { get; init; } = [];
}
