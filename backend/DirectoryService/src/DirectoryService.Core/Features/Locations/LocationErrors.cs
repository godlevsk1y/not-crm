using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Features.Locations;

public static class LocationErrors
{
    public static Error AlreadyExists() =>
        Error.Conflict(new ErrorMessage("location.exists", "Location already exists"));
    
    public static Error AlreadyExists(string name) =>
        Error.Conflict(new ErrorMessage("location.exists", $"Location '{name}' already exists"));
    
    public static Error NotFound() =>
        Error.NotFound(new ErrorMessage("location.not.found", "Location was not found"));
    
    public static Error NotFound(Guid id) =>
        Error.NotFound(new ErrorMessage("location.not.found", $"Location with id '{id}' was not found"));
}