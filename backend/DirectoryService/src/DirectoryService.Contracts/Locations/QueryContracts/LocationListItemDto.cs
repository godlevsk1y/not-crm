using System.ComponentModel;
using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations.QueryContracts;

public record LocationListItemDto(
    [property: Description("Location UUID.")]
    Guid Id,
    [property: Description("Stored location name.")]
    string Name,
    [property: Description("Nested stored address.")]
    AddressDto Address,
    [property: Description("Creation timestamp in UTC.")]
    DateTime CreatedAt,
    [property: Description("Stored department/location link count; may include soft-deleted departments until cleanup.")]
    long DepartmentCount
);
