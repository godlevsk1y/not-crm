using System.ComponentModel;

namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentAncestorDto(
    [property: Description("Ancestor department UUID.")]
    Guid Id,
    [property: Description("Stored department name.")]
    string Name,
    [property: Description("Stored slug.")]
    string Slug,
    [property: Description("Dot-separated hierarchy path.")]
    string Path,
    [property: Description("Depth from root; roots have depth 0.")]
    int Depth,
    [property: Description("Parent UUID; null for a root.")]
    Guid? ParentId
);
