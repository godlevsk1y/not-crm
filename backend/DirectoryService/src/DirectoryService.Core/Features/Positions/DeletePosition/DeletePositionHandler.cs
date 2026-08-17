using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Positions.DeletePosition;

public partial class DeletePositionHandler : ICommandHandler<DeletePositionCommand>
{
    private readonly IPositionsRepository _positionsRepository;
    private readonly ILogger<DeletePositionHandler> _logger;

    public DeletePositionHandler(
        IPositionsRepository positionsRepository,
        ILogger<DeletePositionHandler> logger)
    {
        _positionsRepository = positionsRepository;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        DeletePositionCommand command,
        CancellationToken cancellationToken)
    {
        var position = await _positionsRepository.GetByIdAsync(
            new PositionId(command.Id),
            cancellationToken);
        if (position is null)
        {
            return PositionErrors.NotFound(command.Id);
        }

        await _positionsRepository.DeleteAsync(position, cancellationToken);

        LogPositionDeleted(position.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position deleted with ID {PositionId}")]
    private partial void LogPositionDeleted(Guid positionId);
}
