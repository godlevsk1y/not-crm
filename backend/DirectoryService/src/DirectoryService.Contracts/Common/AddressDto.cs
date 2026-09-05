namespace DirectoryService.Contracts.Common;

public record AddressDto(
    string Country,
    string? Region,
    string City,
    string? District,
    string Street,
    string HouseNumber,
    string? PostalCode
);
