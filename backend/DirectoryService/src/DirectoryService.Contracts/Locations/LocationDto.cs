using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations;

public record LocationDto(
    Guid Id,
    string Name,
    AddressDto Address
);
