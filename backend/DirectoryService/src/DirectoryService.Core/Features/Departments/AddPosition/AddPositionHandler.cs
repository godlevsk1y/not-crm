using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Positions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.AddPosition;

public partial class AddPositionHandler : ICommandHandler<AddPositionCommand>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly IPositionsRepository _positionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<AddPositionHandler> _logger;

    public AddPositionHandler(
        IDepartmentsRepository departmentsRepository,
        IPositionsRepository positionsRepository,
        ITransactionManager transactionManager,
        ILogger<AddPositionHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _positionsRepository = positionsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(AddPositionCommand command, CancellationToken cancellationToken)
    {
        var department = await _departmentsRepository.GetByIdAsync(
            new DepartmentId(command.DepartmentId), cancellationToken);

        if (department is null)
        {
            return DepartmentErrors.NotFound(command.DepartmentId);
        }

        var position = await _positionsRepository.GetByIdAsync(new PositionId(command.PositionId), cancellationToken);
        if (position is null)
        {
            return PositionErrors.NotFound(command.PositionId);
        }

        var departmentPosition = new DepartmentPosition(department.Id, position.Id);

        if (await _departmentsRepository.HasDepartmentPositionAsync(departmentPosition, cancellationToken))
        {
            return DepartmentErrors.PositionAlreadyAdded(command.DepartmentId, command.PositionId);
        }

        await _departmentsRepository.AddPositionAsync(departmentPosition, cancellationToken);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogPositionAdded(position.Id.Value, department.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Position {PositionId} added to department {DepartmentId}")]
    private partial void LogPositionAdded(Guid positionId, Guid departmentId);
}
