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
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionScopeResult.IsFailure)
        {
            return transactionScopeResult.Error;
        }
        using var transaction = transactionScopeResult.Value;
        
        var departmentLocation = await _departmentsRepository.GetDepartmentLocation(
            new DepartmentId(command.DepartmentId), 
            new LocationId(command.LocationId), 
            cancellationToken
        );
        
        if (departmentLocation is null)
        {
            transaction.Rollback();
            return DepartmentErrors.DepartmentLocationNotFound(command.DepartmentId, command.LocationId);
        }
        
        await _departmentsRepository.RemoveLocationAsync(departmentLocation, cancellationToken);
        
        await _transactionManager.SaveChangesAsync(cancellationToken);
        
        transaction.Commit();
        
        LogLocationRemoved(departmentLocation.LocationId, departmentLocation.DepartmentId);
        
        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Location {LocationId} removed from department {DepartmentId}")]
    private partial void LogLocationRemoved(Guid locationId, Guid departmentId);
}