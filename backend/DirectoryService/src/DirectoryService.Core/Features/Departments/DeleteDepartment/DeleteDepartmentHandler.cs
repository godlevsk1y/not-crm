using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Domain.Ids;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.DeleteDepartment;

public partial class DeleteDepartmentHandler : ICommandHandler<DeleteDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ILogger<DeleteDepartmentHandler> _logger;

    public DeleteDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ILogger<DeleteDepartmentHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        DeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var department = await _departmentsRepository.GetByIdAsync(
            new DepartmentId(command.Id),
            cancellationToken);
        if (department is null)
        {
            return DepartmentErrors.NotFound(command.Id);
        }

        await _departmentsRepository.DeleteAsync(department, cancellationToken);

        LogDepartmentDeleted(department.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Department deleted with ID {DepartmentId}")]
    private partial void LogDepartmentDeleted(Guid departmentId);
}
