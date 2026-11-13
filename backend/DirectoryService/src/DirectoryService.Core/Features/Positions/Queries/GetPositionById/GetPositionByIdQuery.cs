using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Queries.GetPositionById;

public record GetPositionByIdQuery(Guid Id) : IQuery;
