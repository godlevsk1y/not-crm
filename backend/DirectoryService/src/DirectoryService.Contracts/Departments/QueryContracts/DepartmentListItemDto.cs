namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentListItemDto(
    Guid Id, 
    string Name,
    string Slug,
    string Path,
    DateTime CreatedAt
);