using System.ComponentModel;

namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentBranchNodeDto(
    [property: Description("Department UUID.")]
    Guid Id,
    [property: Description("Parent UUID; null for a root.")]
    Guid? ParentId,
    [property: Description("Stored department name.")]
    string Name,
    [property: Description("Stored slug.")]
    string Slug,
    [property: Description("Dot-separated hierarchy path.")]
    string Path,
    [property: Description("Depth from root; roots have depth 0.")]
    int Depth,
    [property: Description("Actual creation timestamp in UTC, read from storage.")]
    DateTime CreatedAt,
    [property: Description("Number of all active immediate children, including children outside this branch.")]
    int ChildCount,
    [property: Description("True when childCount is greater than zero.")]
    bool HasChildren
);
