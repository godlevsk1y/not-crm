using System.ComponentModel;

namespace DirectoryService.Contracts.Positions;

public record UpdatePositionRequest(
    [property: Description("Non-whitespace name, trimmed before saving; at most 100 characters after trimming. Required even for PATCH; omitted, null, empty or whitespace: 400.")]
    string Name);
