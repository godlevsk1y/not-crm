using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.CreateDepartment;

public record CreateDepartmentCommand(CreateDepartmentRequest Dto) : ICommand;