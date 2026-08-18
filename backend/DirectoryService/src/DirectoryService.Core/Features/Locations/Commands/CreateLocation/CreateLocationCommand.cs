using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Commands.CreateLocation;

public record CreateLocationCommand(CreateLocationRequest Dto) : ICommand;