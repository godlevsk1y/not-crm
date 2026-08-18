using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.AddLocation;

public record AddLocationCommand(Guid DepartmentId, Guid LocationId) : ICommand;