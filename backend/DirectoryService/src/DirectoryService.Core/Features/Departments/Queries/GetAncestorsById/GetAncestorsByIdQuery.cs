using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetAncestorsById;

public record GetAncestorsByIdQuery(
    Guid Id, 
    int Page = 1, 
    int PageSize = 20
) : IQuery;