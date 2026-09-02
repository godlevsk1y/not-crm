namespace DirectoryService.Contracts.WebApi.Departments;

public record DepartmentDto(Guid Id, string Name, string Slug, string Path, Guid? ParentId);