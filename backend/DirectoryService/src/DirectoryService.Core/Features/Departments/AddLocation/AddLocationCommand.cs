using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.AddLocation;

public record AddLocationCommand(Guid DepartmentId, Guid LocationId) : ICommand;