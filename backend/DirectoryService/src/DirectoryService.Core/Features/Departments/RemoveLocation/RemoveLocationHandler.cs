using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.RemoveLocation;

public partial class RemoveLocationHandler : ICommandHandler<RemoveLocationCommand>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RemoveLocationHandler> _logger;

    public RemoveLocationHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        ILogger<RemoveLocationHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }
    
    public async Task<UnitResult<Error>> Handle(RemoveLocationCommand command, 
        CancellationToken cancellationToken)
    {
        
        var departmentLocation = await _departmentsRepository.GetDepartmentLocation(
            new DepartmentId(command.DepartmentId), 
            new LocationId(command.LocationId), 
            cancellationToken
        );
        
        if (departmentLocation is null)
        {
            return DepartmentErrors.DepartmentLocationNotFound(command.DepartmentId, command.LocationId);
        }
        
        await _departmentsRepository.RemoveLocationAsync(departmentLocation, cancellationToken);
        
        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }
        
        LogLocationRemoved(departmentLocation.LocationId, departmentLocation.DepartmentId);
        
        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Location {LocationId} removed from department {DepartmentId}")]
    private partial void LogLocationRemoved(Guid locationId, Guid departmentId);
}