using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Queries.GetDescendantsById;

public record GetDescendantsByIdQuery(Guid Id) : IQuery;
