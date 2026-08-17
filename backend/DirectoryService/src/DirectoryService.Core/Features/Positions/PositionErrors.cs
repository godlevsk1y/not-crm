using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Features.Positions;

public static class PositionErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(new ErrorMessage(
            "position.not.found",
            $"Position with id '{id}' was not found"));
}
