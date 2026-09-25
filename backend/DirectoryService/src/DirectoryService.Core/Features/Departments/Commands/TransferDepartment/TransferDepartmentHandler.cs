using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Shared.Errors;
using FluentValidation;

namespace DirectoryService.Core.Features.Departments.Commands.TransferDepartment;

public class TransferDepartmentHandler : ICommandHandler<TransferDepartmentCommand, TransferredDepartmentDto>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<TransferDepartmentCommand> _validator;

    public TransferDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        IValidator<TransferDepartmentCommand> validator
    )
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
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

        var departmentId = new DepartmentId(command.DepartmentId);
        var department = await _departmentsRepository.GetByIdAsync(departmentId, cancellationToken);
        if (department is null)
        {
            return DepartmentErrors.NotFound(departmentId);
        }

        if (department.ParentId?.Value == command.NewParentId)
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
        
        if (command.NewParentId is null)
        {
            return await TransferDepartment(department, parent: null, cancellationToken);
        }

        var newParentId = new DepartmentId(command.NewParentId.Value);
        var newParent = await _departmentsRepository.GetByIdAsync(newParentId, cancellationToken);

        if (newParent is null)
        {
            return DepartmentErrors.ParentNotFound(newParentId);
        }

        if (department.Id == newParent.Id)
        {
            return DepartmentErrors.ParentToSelf();
        }

        if (await _departmentsRepository.IsCycle(department, newParent, cancellationToken))
        {
            return DepartmentErrors.Cycle(departmentId, newParentId);
        }
        
        return await TransferDepartment(department, newParent, cancellationToken);
    }

    private async Task<Result<TransferredDepartmentDto, Error>> TransferDepartment(
        Department department, 
        Department? parent,
        CancellationToken cancellationToken)
    {
        var beginTransactionResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (beginTransactionResult.IsFailure)
        {
            return beginTransactionResult.Error;
        }
        
        await using var transaction = beginTransactionResult.Value;

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

        await transaction.CommitAsync(cancellationToken);
        
        return new TransferredDepartmentDto(
            department.Id, 
            department.Name.Value, 
            department.Slug.Value, 
            department.Path.Value, 
            department.Depth,
            department.ParentId?.ToGuid()
        );
    }
}