using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.DeleteDepartment;

public record DeleteDepartmentCommand(Guid Id) : ICommand;
