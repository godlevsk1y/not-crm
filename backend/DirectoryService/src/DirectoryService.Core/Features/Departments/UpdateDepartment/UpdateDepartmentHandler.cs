using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.UpdateDepartment;

public partial class UpdateDepartmentHandler : ICommandHandler<UpdateDepartmentCommand, Guid>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdateDepartmentRequest> _validator;
    private readonly ILogger<UpdateDepartmentHandler> _logger;

    public UpdateDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        IValidator<UpdateDepartmentRequest> validator,
        ILogger<UpdateDepartmentHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<Guid, Error>> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }
        
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken);
        if (transactionScopeResult.IsFailure)
        {
            return transactionScopeResult.Error;
        }
        using var transaction = transactionScopeResult.Value;

        var department = await _departmentsRepository.GetByIdWithParentAsync(
            new DepartmentId(command.Id), cancellationToken);
        if (department is null)
        {
            transaction.Rollback();
            return DepartmentErrors.NotFound(command.Id);
        }

        if (command.Dto.Name is not null)
        {
            var newNameResult = DepartmentName.Create(command.Dto.Name);
            if (newNameResult.IsFailure)
            {
                transaction.Rollback();
                return newNameResult.Error;
            }
            
            department.Rename(newNameResult.Value);
        }

        if (command.Dto.Slug is not null)
        {
            var slugResult = Slug.Create(command.Dto.Slug);
            if (slugResult.IsFailure)
            {
                transaction.Rollback();
                return slugResult.Error;
            }
            
            department.ChangeSlug(slugResult.Value);
        }

        if (command.Dto.ParentId == Guid.Empty)
        {
            var setParentResult = department.SetParent(parent: null);
            if (setParentResult.IsFailure)
            {
                transaction.Rollback();
                return setParentResult.Error;
            }
        }
        else if (command.Dto.ParentId is not null)
        {
            var parentDepartment = await _departmentsRepository.GetByIdAsync(
                new DepartmentId(command.Dto.ParentId.Value), cancellationToken);
            
            if (parentDepartment is null)
            {
                transaction.Rollback();
                return DepartmentErrors.NotFound(command.Dto.ParentId.Value);
            }
        
            var setParentResult = department.SetParent(parentDepartment);
            if (setParentResult.IsFailure)
            {
                transaction.Rollback();
                return setParentResult.Error;
            }
        }
        
        await _transactionManager.SaveChangesAsync(cancellationToken);
        
        transaction.Commit();
        
        LogDepartmentUpdated(department.Id.Value);
        
        return department.Id.Value;
    }

    [LoggerMessage(
        LogLevel.Information, 
        "Department updated with ID {DepartmentId}")]
    private partial void LogDepartmentUpdated(Guid departmentId);
}