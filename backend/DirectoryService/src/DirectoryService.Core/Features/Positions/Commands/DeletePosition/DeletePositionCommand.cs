using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Commands.DeletePosition;

public record DeletePositionCommand(Guid Id) : ICommand;
