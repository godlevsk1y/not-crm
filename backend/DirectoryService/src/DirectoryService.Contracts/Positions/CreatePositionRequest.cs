using System.ComponentModel;

namespace DirectoryService.Contracts.Positions;

public record CreatePositionRequest(
    [property: Description("Non-whitespace name, trimmed before saving; at most 100 characters after trimming. Duplicate names are allowed.")]
    string Name);
