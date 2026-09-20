namespace DirectoryService.Contracts.WebApi.Departments;

public record GetDepartmentTreeRequest(
    int Page = 1, 
    int PageSize = 20
);