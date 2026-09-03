namespace DirectoryService.Contracts.Departments;

public record DepartmentDto(Guid Id, string Name, string Slug, string Path, Guid? ParentId);