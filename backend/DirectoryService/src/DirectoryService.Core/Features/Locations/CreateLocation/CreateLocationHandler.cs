using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Extensions;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Locations.CreateLocation;

public partial class CreateLocationHandler : ICommandHandler<CreateLocationCommand, LocationDto>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly IValidator<CreateLocationRequest> _validator;
    private readonly ILogger<CreateLocationHandler> _logger;

    public CreateLocationHandler(
        ILocationsRepository locationsRepository,
        IValidator<CreateLocationRequest> validator,
        ILogger<CreateLocationHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<LocationDto, Error>> Handle(CreateLocationCommand command, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator
            .ValidateAsync(command.Dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var existingLocation = await _locationsRepository.GetByNameAsync(command.Dto.Name, cancellationToken);
        if (existingLocation is not null)
        {
            return LocationErrors.AlreadyExists(existingLocation.Name.Value);
        }

        var addressResult = Address.Create(
            country: command.Dto.Country,
            region: command.Dto.Region,
            city: command.Dto.City,
            district: command.Dto.District,
            street: command.Dto.Street,
            houseNumber: command.Dto.HouseNumber,
            postalCode: command.Dto.PostalCode
        );
        if (addressResult.IsFailure)
        {
            return addressResult.Error;
        }
        
        var nameResult = LocationName.Create(command.Dto.Name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }
        
        var location = Location.Create(
            nameResult.Value,
            addressResult.Value
        );
        
        await _locationsRepository.AddAsync(location, cancellationToken);
        
        LogLocationCreated(location.Id.Value);
        
        return new LocationDto(
            Id: location.Id,
            Name: location.Name.Value,
            Country: location.Address.Country,
            Region: location.Address.Region,
            City: location.Address.City,
            District: location.Address.District,
            Street: location.Address.Street,
            HouseNumber: location.Address.HouseNumber,
            PostalCode: location.Address.PostalCode
        );
    }
    
    [LoggerMessage(
        LogLevel.Information,
        "Location created with id {LocationId}")]
    private partial void LogLocationCreated(Guid locationId);
}