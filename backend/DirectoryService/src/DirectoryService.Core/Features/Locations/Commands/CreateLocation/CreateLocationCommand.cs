using DirectoryService.Contracts.Locations;
using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Commands.CreateLocation;

public record CreateLocationCommand(CreateLocationRequest Dto) : ICommand;