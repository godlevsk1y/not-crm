using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentTree;

public record GetDepartmentTreeQuery(
    int Page = 1, 
    int PageSize = 20
) : IQuery;