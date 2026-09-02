namespace DirectoryService.Contracts.WebApi.Locations;

public record CreateLocationRequest(
    string Name,
    string Country,
    string? Region,
    string City,
    string? District,
    string Street,
    string HouseNumber,
    string? PostalCode
);