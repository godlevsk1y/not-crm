namespace DirectoryService.Contracts.WebApi.Departments;

public record GetDepartmentTreeByNameRequest
{
    public required string Q { get; set; }

    public int Page { get; set; } = 1;
    
    public int PageSize { get; set; } = 20;
}