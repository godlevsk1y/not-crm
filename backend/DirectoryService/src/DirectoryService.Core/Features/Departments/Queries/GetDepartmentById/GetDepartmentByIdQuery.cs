using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDepartmentById;

public record GetDepartmentByIdQuery(Guid Id) : IQuery;
