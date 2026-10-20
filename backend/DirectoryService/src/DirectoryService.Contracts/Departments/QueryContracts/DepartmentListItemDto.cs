using System.ComponentModel;

namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentListItemDto(
    [property: Description("Department UUID.")]
    Guid Id,
    [property: Description("Stored department name.")]
    string Name,
    [property: Description("Stored slug.")]
    string Slug,
    [property: Description("Dot-separated hierarchy path.")]
    string Path,
    [property: Description("Creation timestamp in UTC.")]
    DateTime CreatedAt
);
