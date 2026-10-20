using System.ComponentModel;

namespace DirectoryService.Contracts.Common;

public record AddressDto(
    [property: Description("Stored address component.")]
    string Country,
    [property: Description("Stored address component.")]
    string? Region,
    [property: Description("Stored address component.")]
    string City,
    [property: Description("Stored address component.")]
    string? District,
    [property: Description("Stored address component.")]
    string Street,
    [property: Description("Stored address component.")]
    string HouseNumber,
    [property: Description("Stored address component.")]
    string? PostalCode
);
