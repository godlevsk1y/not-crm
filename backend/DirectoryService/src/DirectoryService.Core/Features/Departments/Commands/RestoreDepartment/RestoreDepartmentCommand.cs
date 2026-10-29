using Shared.Core.Abstractions;

namespace DirectoryService.Core.Features.Departments.Commands.RestoreDepartment;

public record RestoreDepartmentCommand(Guid Id) : ICommand;
