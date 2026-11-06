using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.ValueObjects;
using Shared.Kernel.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shared.Core.Abstractions;
using Shared.Core.Database;
using Shared.Core.Extensions;

namespace DirectoryService.Core.Features.Departments.Commands.UpdateDepartment;

public partial class UpdateDepartmentHandler : ICommandHandler<UpdateDepartmentCommand, Guid>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<UpdateDepartmentRequest> _validator;
    private readonly ILogger<UpdateDepartmentHandler> _logger;
    private readonly DepartmentChildrenCache _cache;

    public UpdateDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ITransactionManager transactionManager,
        IValidator<UpdateDepartmentRequest> validator,
        ILogger<UpdateDepartmentHandler> logger,
        DepartmentChildrenCache cache)
    {
        _departmentsRepository = departmentsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
        _cache = cache;
    }
    
    public async Task<Result<Guid, Error>> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        List<Guid> affectedParentIds = [];
        var result = await _transactionManager.ExecuteAsync<Guid>(async ct =>
        {
            var department = await _departmentsRepository.GetByIdWithParentAsync(
                new DepartmentId(command.Id), ct);
            if (department is null)
            {
                return DepartmentErrors.NotFound(command.Id);
            }

            var oldPath = department.Path;
            var oldName = department.Name;
            var oldSlug = department.Slug;

            if (command.Dto.Name is not null)
            {
                var newNameResult = DepartmentName.Create(command.Dto.Name);
                if (newNameResult.IsFailure)
                {
                    return newNameResult.Error;
                }

                department.Rename(newNameResult.Value);
            }

            if (command.Dto.Slug is not null)
            {
                var slugResult = Slug.Create(command.Dto.Slug);
                if (slugResult.IsFailure)
                {
                    return slugResult.Error;
                }

                department.ChangeSlug(slugResult.Value);
            }

            if (oldName != department.Name || oldSlug != department.Slug)
            {
                var subtreeIds = await _departmentsRepository.GetSubtreeIdsAsync(oldPath, ct);
                affectedParentIds.AddRange(subtreeIds.Select(id => id.Value));
                if (department.ParentId is not null)
                {
                    affectedParentIds.Add(department.ParentId.Value);
                }
            }

            var saveResult = await _transactionManager.SaveChangesAsync(ct);
            if (saveResult.IsFailure)
            {
                return saveResult.Error;
            }

            if (oldPath != department.Path)
            {
                await _departmentsRepository.RecalculatePathsAsync(oldPath, department.Path, ct);
            }

            return department.Id.Value;
        }, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error;
        }

        if (affectedParentIds.Count > 0)
        {
            await _cache.InvalidateAsync(affectedParentIds, cancellationToken);
        }

        LogDepartmentUpdated(result.Value);
        
        return result.Value;
    }

    [LoggerMessage(
        LogLevel.Information, 
        "Department {DepartmentId} updated")]
    private partial void LogDepartmentUpdated(Guid departmentId);
}
