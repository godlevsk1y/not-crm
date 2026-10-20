namespace DirectoryService.Contracts.Positions.QueryContracts;

public record PositionListItemDto(
    Guid Id,
    string Name,
    DateTime CreatedAt
);
