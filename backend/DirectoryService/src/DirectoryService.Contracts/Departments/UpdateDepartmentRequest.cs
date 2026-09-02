namespace DirectoryService.Contracts.WebApi.Departments;

public record UpdateDepartmentRequest(string? Name, string? Slug, Guid? ParentId);