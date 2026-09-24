using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetChildrenByParentId;

public record GetChildrenByParentIdQuery(Guid ParentId, int Page = 1, int PageSize = 20) : IQuery;