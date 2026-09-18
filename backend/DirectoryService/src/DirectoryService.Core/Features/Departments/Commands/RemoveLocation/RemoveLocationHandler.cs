using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.Commands.RemoveLocation;

public partial class RemoveLocationHandler : ICommandHandler<RemoveLocationCommand>
{
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RemoveLocationHandler> _logger;

    public RemoveLocationHandler(
        IDepartmentLocationsRepository departmentLocationsRepository,
        ITransactionManager transactionManager,
        ILogger<RemoveLocationHandler> logger)
    {
        _departmentLocationsRepository = departmentLocationsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }
    
    public async Task<UnitResult<Error>> Handle(RemoveLocationCommand command, 
        CancellationToken cancellationToken)
    {
        
        var departmentLocation = await _departmentLocationsRepository.GetAsync(
            new DepartmentId(command.DepartmentId), 
            new LocationId(command.LocationId), 
            cancellationToken
        );
        
        if (departmentLocation is null)
        {
            return DepartmentErrors.DepartmentLocationNotFound(command.DepartmentId, command.LocationId);
        }
        
        _departmentLocationsRepository.Remove(departmentLocation);
        
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
