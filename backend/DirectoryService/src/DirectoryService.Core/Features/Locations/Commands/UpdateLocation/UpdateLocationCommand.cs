using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Commands.UpdateLocation;

public record UpdateLocationCommand(Guid Id, UpdateLocationRequest Dto) : ICommand;