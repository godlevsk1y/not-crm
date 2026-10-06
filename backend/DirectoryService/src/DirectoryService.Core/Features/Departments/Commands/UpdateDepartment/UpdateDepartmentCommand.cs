using DirectoryService.Contracts.Departments;
using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;

public record UpdateDepartmentCommand(Guid Id, UpdateDepartmentRequest Dto) : ICommand;