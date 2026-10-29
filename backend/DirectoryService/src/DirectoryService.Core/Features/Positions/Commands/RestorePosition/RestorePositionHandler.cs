using CSharpFunctionalExtensions;
using DirectoryService.Domain.Ids;
using Shared.Kernel.Errors;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;

namespace DirectoryService.Core.Features.Positions.Commands.RestorePosition;

public partial class RestorePositionHandler : ICommandHandler<RestorePositionCommand>
{
    private readonly IPositionsRepository _positionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RestorePositionHandler> _logger;

    public RestorePositionHandler(
        IPositionsRepository positionsRepository,
        ITransactionManager transactionManager,
        ILogger<RestorePositionHandler> logger)
    {
        _positionsRepository = positionsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        RestorePositionCommand command,
        CancellationToken cancellationToken)
    {
        var position = await _positionsRepository.GetByIdIncludingDeletedAsync(
            new PositionId(command.Id),
            cancellationToken);
        if (position is null)
        {
            return PositionErrors.NotFound(command.Id);
        }

        if (position.DeletedAt is null)
        {
            return UnitResult.Success<Error>();
        }

        position.Restore();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogPositionRestored(position.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position restored with ID {PositionId}")]
    private partial void LogPositionRestored(Guid positionId);
}
