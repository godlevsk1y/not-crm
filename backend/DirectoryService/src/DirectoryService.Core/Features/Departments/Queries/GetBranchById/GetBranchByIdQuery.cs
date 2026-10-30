using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetBranchById;

public record GetBranchByIdQuery(Guid Id) : IQuery;
