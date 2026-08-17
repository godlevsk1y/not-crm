using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Features.Departments;

public static class DepartmentErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(new ErrorMessage("department.not.found", $"Department with id '{id}' was not found"));
    
    public static Error LocationAlreadyAdded(Guid departmentId, Guid locationId) =>
        Error.Conflict(new ErrorMessage(
            "department.location.already.added", 
            $"Department with id '{departmentId}' already has Location with id '{locationId}' added"));

    public static Error PositionAlreadyAdded(Guid departmentId, Guid positionId) =>
        Error.Conflict(new ErrorMessage(
            "department.position.already.added",
            $"Department with id '{departmentId}' already has Position with id '{positionId}' added"));
    
    public static Error DepartmentLocationNotFound(Guid departmentId, Guid locationId) =>
        Error.NotFound(new ErrorMessage("department.location.not.found",
            $"Department with id '{departmentId}' does not have Location with id '{locationId}'"));
}
