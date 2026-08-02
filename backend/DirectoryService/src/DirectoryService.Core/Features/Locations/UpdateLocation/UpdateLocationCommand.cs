using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.UpdateLocation;

public record UpdateLocationCommand(Guid Id, UpdateLocationRequest Dto) : ICommand;