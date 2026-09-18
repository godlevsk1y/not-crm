using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Locations.Commands.DeleteLocation;

public partial class DeleteLocationHandler : ICommandHandler<DeleteLocationCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DeleteLocationHandler> _logger;

    public DeleteLocationHandler(
        ILocationsRepository locationsRepository,
        IDepartmentLocationsRepository departmentLocationsRepository,
        ITransactionManager transactionManager,
        ILogger<DeleteLocationHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _departmentLocationsRepository = departmentLocationsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteLocationCommand command,
        CancellationToken cancellationToken)
    {
        var location = await _locationsRepository.GetByIdAsync(
            new LocationId(command.Id),
            cancellationToken);
        if (location is null)
        {
            return LocationErrors.NotFound(command.Id);
        }

        var beginTransactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransactionResult.IsFailure)
        {
            return beginTransactionResult.Error;
        }

        await using var transaction = beginTransactionResult.Value;

        await _departmentLocationsRepository.RemoveAllByLocationIdAsync(location.Id, cancellationToken);
        
        location.SoftDelete();

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

        LogLocationDeleted(location.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Location deleted with ID {LocationId}")]
    private partial void LogLocationDeleted(Guid locationId);
}
