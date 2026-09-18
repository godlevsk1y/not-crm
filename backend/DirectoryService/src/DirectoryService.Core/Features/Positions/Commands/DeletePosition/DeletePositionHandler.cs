using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Positions.Commands.DeletePosition;

public partial class DeletePositionHandler : ICommandHandler<DeletePositionCommand>
{
    private readonly IPositionsRepository _positionsRepository;
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DeletePositionHandler> _logger;

    public DeletePositionHandler(
        IPositionsRepository positionsRepository,
        IDepartmentPositionsRepository departmentPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<DeletePositionHandler> logger)
    {
        _positionsRepository = positionsRepository;
        _departmentPositionsRepository = departmentPositionsRepository;
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

        var beginTransactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransactionResult.IsFailure)
        {
            return beginTransactionResult.Error;
        }
        
        await using var transaction = beginTransactionResult.Value;
        
        await _departmentPositionsRepository.RemoveAllByPositionIdAsync(position.Id, cancellationToken);
        
        position.SoftDelete();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        var commitResult = await transaction.CommitAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult.Error;
        }

        LogPositionDeleted(position.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position deleted with ID {PositionId}")]
    private partial void LogPositionDeleted(Guid positionId);
}
