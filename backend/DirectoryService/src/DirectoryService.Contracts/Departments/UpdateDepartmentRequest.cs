namespace DirectoryService.Contracts.Departments;

public record UpdateDepartmentRequest(string? Name, string? Slug, Guid? ParentId);