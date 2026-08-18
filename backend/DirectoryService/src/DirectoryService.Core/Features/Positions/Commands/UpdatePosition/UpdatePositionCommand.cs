using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Commands.UpdatePosition;

public record UpdatePositionCommand(Guid Id, UpdatePositionRequest Dto) : ICommand;
