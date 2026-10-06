using DirectoryService.Contracts.Positions;
using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Commands.CreatePosition;

public record CreatePositionCommand(CreatePositionRequest Dto) : ICommand;
