using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Commands.RestorePosition;

public record RestorePositionCommand(Guid Id) : ICommand;
