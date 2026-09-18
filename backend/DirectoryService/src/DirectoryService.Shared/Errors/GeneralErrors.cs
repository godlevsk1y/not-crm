namespace DirectoryService.Shared.Errors;

public static class GeneralErrors
{
    public static Error Internal() =>
        Error.Internal(new ErrorMessage("internal.server.error", "Internal error"));
}