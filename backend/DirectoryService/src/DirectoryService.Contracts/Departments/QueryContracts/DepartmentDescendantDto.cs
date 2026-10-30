using System.ComponentModel;

namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentDescendantDto(
    [property: Description("Descendant department UUID.")]
    Guid Id,
    [property: Description("Stored department name.")]
    string Name,
    [property: Description("Immediate parent department UUID.")]
    Guid? ParentId
);
