using System.ComponentModel;

namespace DirectoryService.Contracts.Departments;

public record DepartmentDto(
    [property: Description("Department UUID.")]
    Guid Id,
    [property: Description("Stored department name.")]
    string Name,
    [property: Description("Stored slug.")]
    string Slug,
    [property: Description("Dot-separated hierarchy path; a root path equals its slug.")]
    string Path,
    [property: Description("Parent UUID; null for a root.")]
    Guid? ParentId);
