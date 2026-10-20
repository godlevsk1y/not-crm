using System.ComponentModel;
using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public record LocationDto(
    [property: Description("Location UUID.")]
    Guid Id,
    [property: Description("Stored location name.")]
    string Name,
    [property: Description("Nested stored address; request bodies use flat address fields.")]
    AddressDto Address
);
