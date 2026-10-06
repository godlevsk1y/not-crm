using Shared.Kernel.Errors;

namespace DirectoryService.Domain.Models.Errors;

public static class ModelErrors
{
    public static class Department
    {
        public static Error NameEmpty()
            => Error.Domain(new ErrorMessage("department.name.empty", "Department name cannot be empty"));
        
        public static Error ParentToItself()
            => Error.Domain(new ErrorMessage("department.parent.itself", "Department cannot be parented to itself"));
    }

    public static class Location
    {
        public static Error NameEmpty()
            => Error.Domain(new ErrorMessage("location.name.empty", "Location name cannot be empty"));
    }
    
    public static class Position
    {
        public static Error NameEmpty()
            => Error.Domain(new ErrorMessage("position.name.empty", "Position name cannot be empty"));
    }
}