using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.UpdatePosition;

public record UpdatePositionCommand(Guid Id, UpdatePositionRequest Dto) : ICommand;
