using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.Commands.RemovePosition;

public partial class RemovePositionHandler : ICommandHandler<RemovePositionCommand>
{
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RemovePositionHandler> _logger;

    public RemovePositionHandler(
        IDepartmentPositionsRepository departmentPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<RemovePositionHandler> logger)
    {
        _departmentPositionsRepository = departmentPositionsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(RemovePositionCommand command,
        CancellationToken cancellationToken)
    {
        var departmentPosition = await _departmentPositionsRepository.GetAsync(
            new DepartmentId(command.DepartmentId),
            new PositionId(command.PositionId),
            cancellationToken
        );

        if (departmentPosition is null)
        {
            return DepartmentErrors.DepartmentPositionNotFound(command.DepartmentId, command.PositionId);
        }

        _departmentPositionsRepository.Remove(departmentPosition);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogPositionRemoved(departmentPosition.PositionId, departmentPosition.DepartmentId);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position {PositionId} removed from department {DepartmentId}")]
    private partial void LogPositionRemoved(Guid positionId, Guid departmentId);
}
