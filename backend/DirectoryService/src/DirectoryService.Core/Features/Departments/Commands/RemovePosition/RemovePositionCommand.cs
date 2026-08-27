using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.RemovePosition;

public record RemovePositionCommand(Guid DepartmentId, Guid PositionId) : ICommand;
