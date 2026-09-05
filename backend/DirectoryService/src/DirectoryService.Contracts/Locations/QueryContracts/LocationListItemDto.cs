using DirectoryService.Contracts.Common;

namespace DirectoryService.Contracts.Locations.QueryContracts;

public record LocationListItemDto(
    Guid Id,
    string Name,
    AddressDto Address,
    DateTime CreatedAt,
    long DepartmentCount
);