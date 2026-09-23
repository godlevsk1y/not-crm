using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentTreeByName;

public record GetDepartmentTreeByNameQuery(
    string Search,
    int Page = 1, 
    int PageSize = 20
) : IQuery;