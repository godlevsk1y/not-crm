using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Features.Locations;

public interface ILocationsService
{
    Task<Result<LocationDto, Error>> CreateAsync(CreateLocationRequest dto, CancellationToken cancellationToken);

    Task<Result<Guid, Error>> UpdateAsync(Guid id, UpdateLocationRequest dto, CancellationToken cancellationToken);
}