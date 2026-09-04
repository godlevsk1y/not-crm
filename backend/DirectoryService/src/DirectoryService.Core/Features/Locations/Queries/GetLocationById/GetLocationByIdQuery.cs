using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Queries.GetLocationById;

public record GetLocationByIdQuery(Guid Id) : IQuery;
