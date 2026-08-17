using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.AddPosition;

public record AddPositionCommand(Guid DepartmentId, Guid PositionId) : ICommand;
