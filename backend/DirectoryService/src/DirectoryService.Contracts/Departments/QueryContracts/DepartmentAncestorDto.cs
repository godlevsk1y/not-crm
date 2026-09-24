namespace DirectoryService.Contracts.Departments.QueryContracts;

public record DepartmentAncestorDto(
    Guid Id, 
    string Name, 
    string Slug,
    string Path, 
    int Depth, 
    Guid? ParentId
);