using System.ComponentModel;

namespace DirectoryService.Contracts.Locations.QueryContracts;

public record LocationWithDepartmentCountDto(
    [property: Description("Location with nested address.")]
    LocationDto Location,
    [property: Description("Stored department/location link count; may include soft-deleted departments until cleanup.")]
    long DepartmentCount);
