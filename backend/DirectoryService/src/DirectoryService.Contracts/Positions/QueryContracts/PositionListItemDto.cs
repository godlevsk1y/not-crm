using System.ComponentModel;

namespace DirectoryService.Contracts.Positions.QueryContracts;

public record PositionListItemDto(
    [property: Description("Position UUID.")]
    Guid Id,
    [property: Description("Stored position name.")]
    string Name,
    [property: Description("Creation timestamp in UTC.")]
    DateTime CreatedAt
);
