using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Queries.GetLocationById;

public record GetLocationByIdQuery(Guid Id) : IQuery;
