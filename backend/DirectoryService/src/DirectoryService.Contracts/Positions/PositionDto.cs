using System.ComponentModel;

namespace DirectoryService.Contracts.Positions;

public record PositionDto(
    [property: Description("Position UUID.")]
    Guid Id,
    [property: Description("Stored position name.")]
    string Name);
