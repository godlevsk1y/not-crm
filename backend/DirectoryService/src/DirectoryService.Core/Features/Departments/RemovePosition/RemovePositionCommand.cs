using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.RemovePosition;

public record RemovePositionCommand(Guid DepartmentId, Guid PositionId) : ICommand;
