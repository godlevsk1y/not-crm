using CSharpFunctionalExtensions;
using DirectoryService.Domain.Ids;
using Shared.Kernel.Errors;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;

namespace DirectoryService.Core.Features.Locations.Commands.RestoreLocation;

public partial class RestoreLocationHandler : ICommandHandler<RestoreLocationCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RestoreLocationHandler> _logger;

    public RestoreLocationHandler(
        ILocationsRepository locationsRepository,
        ITransactionManager transactionManager,
        ILogger<RestoreLocationHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        RestoreLocationCommand command,
        CancellationToken cancellationToken)
    {
        var location = await _locationsRepository.GetByIdIncludingDeletedAsync(
            new LocationId(command.Id),
            cancellationToken);
        if (location is null)
        {
            return LocationErrors.NotFound(command.Id);
        }

        if (location.DeletedAt is null)
        {
            return UnitResult.Success<Error>();
        }

        location.Restore();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogLocationRestored(location.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Location restored with ID {LocationId}")]
    private partial void LogLocationRestored(Guid locationId);
}
