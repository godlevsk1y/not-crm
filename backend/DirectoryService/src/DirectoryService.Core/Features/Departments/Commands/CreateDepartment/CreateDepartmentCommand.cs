using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.CreateDepartment;

public record CreateDepartmentCommand(CreateDepartmentRequest Dto) : ICommand;