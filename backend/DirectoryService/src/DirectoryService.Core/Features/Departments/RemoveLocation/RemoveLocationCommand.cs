using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.RemoveLocation;

public record RemoveLocationCommand(Guid DepartmentId, Guid LocationId) : ICommand;