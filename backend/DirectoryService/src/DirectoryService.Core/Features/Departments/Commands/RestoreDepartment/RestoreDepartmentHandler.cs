using CSharpFunctionalExtensions;
using DirectoryService.Domain.Ids;
using Shared.Kernel.Errors;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;

namespace DirectoryService.Core.Features.Departments.Commands.RestoreDepartment;

public partial class RestoreDepartmentHandler : ICommandHandler<RestoreDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RestoreDepartmentHandler> _logger;

    public RestoreDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        ILogger<RestoreDepartmentHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(
        RestoreDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var department = await _departmentsRepository.GetByIdIncludingDeletedAsync(
            new DepartmentId(command.Id),
            cancellationToken);
        if (department is null)
        {
            return DepartmentErrors.NotFound(command.Id);
        }

        if (department.DeletedAt is null)
        {
            return UnitResult.Success<Error>();
        }

        if (department.Parent?.DeletedAt is not null)
        {
            return DepartmentErrors.ParentArchived(department.Parent.Id);
        }

        department.Restore();

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogDepartmentRestored(department.Id.Value);

        return UnitResult.Success<Error>();
    }

    [LoggerMessage(
        LogLevel.Information,
        "Department restored with ID {DepartmentId}")]
    private partial void LogDepartmentRestored(Guid departmentId);
}
