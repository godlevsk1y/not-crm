using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Locations.DeleteLocation;

public partial class DeleteLocationHandler : ICommandHandler<DeleteLocationCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILogger<DeleteLocationHandler> _logger;

    public DeleteLocationHandler(
        ILocationsRepository locationsRepository,
        ILogger<DeleteLocationHandler> logger)
    {
        _locationsRepository = locationsRepository;
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

        await _locationsRepository.DeleteAsync(location, cancellationToken);

        LogLocationDeleted(location.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Location deleted with ID {LocationId}")]
    private partial void LogLocationDeleted(Guid locationId);
}
