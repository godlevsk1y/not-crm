using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Database;
using DirectoryService.Shared.Errors;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Core.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQueryHandler
{
    private readonly IReadDbContext _readDbContext;

    public GetLocationByIdQueryHandler(IReadDbContext readDbContext)
    {
        _readDbContext = readDbContext;
    }

    public async Task<Result<LocationDto, Error>> Handle(GetLocationByIdQuery query, CancellationToken cancellationToken)
    {
        var location = await _readDbContext.LocationsRead
            .FirstOrDefaultAsync(l => l.Id == query.Id, cancellationToken);

        if (location is null)
            return LocationErrors.NotFound(query.Id);

        return new LocationDto(
            Id: location.Id,
            Name: location.Name.ToString(),
            Country: location.Address.Country,
            Region: location.Address.Region,
            City: location.Address.City,
            District: location.Address.District,
            Street: location.Address.Street,
            HouseNumber: location.Address.HouseNumber,
            PostalCode: location.Address.PostalCode
        );
    }
}