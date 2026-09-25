namespace DirectoryService.Contracts.Departments;

public record TransferredDepartmentDto(Guid Id, string Name, string Slug, string Path, int Depth, Guid? ParentId);