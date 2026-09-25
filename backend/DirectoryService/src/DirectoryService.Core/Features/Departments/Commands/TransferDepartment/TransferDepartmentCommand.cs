using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.TransferDepartment;

public record TransferDepartmentCommand(
    Guid DepartmentId, 
    Guid? NewParentId
) : ICommand;