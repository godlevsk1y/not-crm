using System.ComponentModel;

namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentNodeDto
{
    [Description("Department UUID.")]
    public Guid Id { get; init; }
    
    [Description("Stored department name.")]
    public string Name { get; init; } = null!;
    
    [Description("Stored slug.")]
    public string Slug { get; init; } = null!;
    
    [Description("Dot-separated hierarchy path.")]
    public string Path { get; init; } = null!;
    
    [Description("Depth from root; roots have depth 0.")]
    public int Depth { get; init; }
    
    [Description("Currently the default DateTime (0001-01-01T00:00:00): the root/children queries do not select created_at.")]
    public DateTime CreatedAt { get; init; }
    
    [Description("True when active immediate children exist.")]
    public bool HasChildren { get; init; }
    
    [Description("Number of active immediate children.")]
    public int ChildCount { get; init; }
}
