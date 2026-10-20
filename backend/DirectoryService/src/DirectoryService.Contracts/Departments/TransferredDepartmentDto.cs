using System.ComponentModel;

namespace DirectoryService.Contracts.Departments;

public record TransferredDepartmentDto(
    [property: Description("Transferred department UUID.")]
    Guid Id,
    [property: Description("Stored department name.")]
    string Name,
    [property: Description("Stored slug.")]
    string Slug,
    [property: Description("Updated dot-separated hierarchy path.")]
    string Path,
    [property: Description("Updated depth from root; roots have depth 0.")]
    int Depth,
    [property: Description("New parent UUID; null for a root.")]
    Guid? ParentId);
