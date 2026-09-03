using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;

public record UpdateDepartmentCommand(Guid Id, UpdateDepartmentRequest Dto) : ICommand;