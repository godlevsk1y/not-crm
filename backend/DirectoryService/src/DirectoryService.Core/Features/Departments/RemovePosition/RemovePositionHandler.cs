using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.RemovePosition;

public partial class RemovePositionHandler : ICommandHandler<RemovePositionCommand>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RemovePositionHandler> _logger;

    public RemovePositionHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        ILogger<RemovePositionHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(RemovePositionCommand command,
        CancellationToken cancellationToken)
    {
        var departmentPosition = await _departmentsRepository.GetDepartmentPosition(
            new DepartmentId(command.DepartmentId),
            new PositionId(command.PositionId),
            cancellationToken
        );

        if (departmentPosition is null)
        {
            return DepartmentErrors.DepartmentPositionNotFound(command.DepartmentId, command.PositionId);
        }

        _departmentsRepository.RemovePosition(departmentPosition);

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
