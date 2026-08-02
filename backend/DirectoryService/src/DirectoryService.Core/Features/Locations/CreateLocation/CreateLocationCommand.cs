using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.CreateLocation;

public record CreateLocationCommand(CreateLocationRequest Dto) : ICommand;