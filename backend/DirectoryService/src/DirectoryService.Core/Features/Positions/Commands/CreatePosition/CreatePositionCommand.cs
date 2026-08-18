using DirectoryService.Contracts.WebApi.Positions;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Positions.Commands.CreatePosition;

public record CreatePositionCommand(CreatePositionRequest Dto) : ICommand;
