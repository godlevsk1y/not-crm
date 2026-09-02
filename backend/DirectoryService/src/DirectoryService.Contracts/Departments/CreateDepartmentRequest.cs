namespace DirectoryService.Contracts.WebApi.Departments;

public record CreateDepartmentRequest(string Name, string Slug, 
    IReadOnlyCollection<Guid> LocationIds, Guid? ParentId = null);