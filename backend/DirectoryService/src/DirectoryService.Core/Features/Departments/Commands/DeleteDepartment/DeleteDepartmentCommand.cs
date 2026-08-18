using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.DeleteDepartment;

public record DeleteDepartmentCommand(Guid Id) : ICommand;
