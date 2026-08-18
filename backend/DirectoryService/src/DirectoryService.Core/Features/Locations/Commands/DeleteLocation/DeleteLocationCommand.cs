using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.Commands.DeleteLocation;

public record DeleteLocationCommand(Guid Id) : ICommand;
