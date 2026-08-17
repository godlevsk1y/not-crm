using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Positions.DeletePosition;

public partial class DeletePositionHandler : ICommandHandler<DeletePositionCommand>
{
    private readonly IPositionsRepository _positionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DeletePositionHandler> _logger;

    public DeletePositionHandler(
        IPositionsRepository positionsRepository,
        ITransactionManager transactionManager,
        ILogger<DeletePositionHandler> logger)
    {
        _positionsRepository = positionsRepository;
        _transactionManager = transactionManager;
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

        _positionsRepository.Delete(position);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogPositionDeleted(position.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position deleted with ID {PositionId}")]
    private partial void LogPositionDeleted(Guid positionId);
}
