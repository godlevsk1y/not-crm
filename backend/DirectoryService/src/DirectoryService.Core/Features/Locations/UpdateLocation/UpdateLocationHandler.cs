using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Extensions;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Locations.UpdateLocation;

public partial class UpdateLocationHandler : ICommandHandler<UpdateLocationCommand, Guid>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly IValidator<UpdateLocationRequest> _validator;
    private readonly ILogger<UpdateLocationHandler> _logger;

    public UpdateLocationHandler(
        ILocationsRepository locationsRepository,
        IValidator<UpdateLocationRequest> validator,
        ILogger<UpdateLocationHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<Guid, Error>> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command.Dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var location = await _locationsRepository.GetByIdAsync(command.Id, cancellationToken);
        if (location is null)
        {
            return LocationErrors.NotFound(command.Id);
        }

        var newRegion = command.Dto.Region?.Length == 0 ? null : command.Dto.Region;
        var newDistrict = command.Dto.District?.Length == 0 ? null : command.Dto.District;
        var newPostalCode = command.Dto.PostalCode?.Length == 0 ? null : command.Dto.PostalCode;
        
        var newAddressResult = Address.Create(
            country: command.Dto.Country ?? location.Address.Country,
            region: newRegion ?? location.Address.Region,
            city: command.Dto.City ?? location.Address.City,
            district: newDistrict ?? location.Address.District,
            street: command.Dto.Street ?? location.Address.Street,
            houseNumber: command.Dto.HouseNumber ?? location.Address.HouseNumber,
            postalCode: newPostalCode ?? location.Address.PostalCode
        );
        if (newAddressResult.IsFailure)
        {
            return newAddressResult.Error;
        }

        var newName = location.Name;
        if (command.Dto.Name is not null)
        {
            var newNameResult = LocationName.Create(command.Dto.Name);
            if (newNameResult.IsFailure)
            {
                return newNameResult.Error;
            }
            
            newName = newNameResult.Value;
        }
        
        location.Update(newName, newAddressResult.Value);
        
        await _locationsRepository.SaveAsync(cancellationToken);
        
        LogLocationUpdated(location.Id.Value);
        
        return location.Id.Value;
    }

    [LoggerMessage(
        LogLevel.Information, 
        "Location updated with id {LocationId}")]
    private partial void LogLocationUpdated(Guid locationId);
}