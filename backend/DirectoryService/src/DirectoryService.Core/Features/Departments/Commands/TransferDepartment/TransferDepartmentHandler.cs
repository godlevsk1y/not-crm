using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Features.Departments.Caching;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using Shared.Kernel.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Extensions;

namespace DirectoryService.Core.Features.Departments.Commands.TransferDepartment;

public partial class TransferDepartmentHandler : ICommandHandler<TransferDepartmentCommand, TransferredDepartmentDto>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<TransferDepartmentCommand> _validator;
    private readonly ILogger<TransferDepartmentHandler> _logger;
    private readonly DepartmentChildrenCache _cache;

    public TransferDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        IValidator<TransferDepartmentCommand> validator,
        ILogger<TransferDepartmentHandler> logger,
        DepartmentChildrenCache cache
    )
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<Result<TransferredDepartmentDto, Error>> Handle(
        TransferDepartmentCommand command, 
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        Guid? oldParentId = null;
        var parentChanged = false;
        var result = await _transactionManager.ExecuteAsync(async ct =>
        {
            var departmentId = new DepartmentId(command.DepartmentId);
            var department = await _departmentsRepository.GetByIdWithDescendantsLockAsync(departmentId, ct);
            if (department is null)
            {
                return DepartmentErrors.NotFound(departmentId);
            }

            oldParentId = department.ParentId?.Value;
            if (oldParentId == command.NewParentId)
            {
                return new TransferredDepartmentDto(
                    department.Id, 
                    department.Name.Value, 
                    department.Slug.Value, 
                    department.Path.Value, 
                    department.Depth,
                    department.ParentId?.ToGuid()
                );
            }
        
            parentChanged = true;
            if (command.NewParentId is null)
            {
                return await TransferDepartment(department, parent: null, ct);
            }

            var newParentId = new DepartmentId(command.NewParentId.Value);
            var newParent = await _departmentsRepository.GetByIdWithLockAsync(newParentId, ct);

            if (newParent is null)
            {
                return DepartmentErrors.ParentNotFound(newParentId);
            }

            if (department.Id == newParent.Id)
            {
                return DepartmentErrors.ParentToSelf();
            }

            if (await _departmentsRepository.IsCycle(department, newParent, ct))
            {
                return DepartmentErrors.Cycle(departmentId, newParentId);
            }
        
            return await TransferDepartment(department, newParent, ct);
        }, cancellationToken);

        if (result.IsSuccess && parentChanged)
        {
            await _cache.InvalidateAllAsync(cancellationToken);

            LogDepartmentTransferred(command.DepartmentId, oldParentId, command.NewParentId);
        }

        return result;
    }

    private async Task<Result<TransferredDepartmentDto, Error>> TransferDepartment(
        Department department, 
        Department? parent,
        CancellationToken cancellationToken)
    {
        var oldPath = department.Path;
        
        var setParentResult = department.SetParent(parent);
        if (setParentResult.IsFailure)
        {
            return setParentResult.Error;
        }

        var newPath = department.Path;

        var saveChangesResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveChangesResult.IsFailure)
        {
            return saveChangesResult.Error;
        }

        await _departmentsRepository.RecalculatePathsAsync(oldPath, newPath, cancellationToken);
        
        return new TransferredDepartmentDto(
            department.Id, 
            department.Name.Value, 
            department.Slug.Value, 
            department.Path.Value, 
            department.Depth,
            department.ParentId?.ToGuid()
        );
    }

    [LoggerMessage(
        LogLevel.Information,
        "Department {DepartmentId} transferred from parent {OldParentId} to parent {NewParentId}")]
    private partial void LogDepartmentTransferred(Guid departmentId, Guid? oldParentId, Guid? newParentId);
}
