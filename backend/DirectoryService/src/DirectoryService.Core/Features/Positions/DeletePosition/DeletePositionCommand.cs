using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.DeletePosition;

public record DeletePositionCommand(Guid Id) : ICommand;
