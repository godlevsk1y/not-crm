namespace DirectoryService.Contracts.Departments;

public record CreateDepartmentRequest(string Name, string Slug, 
    IReadOnlyCollection<Guid> LocationIds, Guid? ParentId = null);