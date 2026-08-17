using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Locations.DeleteLocation;

public record DeleteLocationCommand(Guid Id) : ICommand;
