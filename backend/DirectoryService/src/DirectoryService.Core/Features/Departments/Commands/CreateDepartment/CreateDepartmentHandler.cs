using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Extensions;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.Commands.CreateDepartment;

public partial class CreateDepartmentHandler : ICommandHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ILocationsRepository _locationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly IValidator<CreateDepartmentRequest> _validator;
    private readonly ILogger<CreateDepartmentHandler> _logger;

    public CreateDepartmentHandler(
        IDepartmentsRepository departmentsRepository,
        ILocationsRepository locationsRepository,
        ITransactionManager transactionManager,
        IValidator<CreateDepartmentRequest> validator,
        ILogger<CreateDepartmentHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _locationsRepository = locationsRepository;
        _transactionManager = transactionManager;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<Result<DepartmentDto, Error>> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command.Dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var locations = await _locationsRepository.GetByIdsAsync(
            [..command.Dto.LocationIds.Select(id => new LocationId(id))], 
            cancellationToken
        );

        if (locations.Count != command.Dto.LocationIds.Count)
        {
            var foundIds = locations
                .Select(l => l.Id.Value)
                .ToHashSet();

            var missingId = command.Dto.LocationIds
                .First(id => !foundIds.Contains(id));
            
            return LocationErrors.NotFound(missingId);
        }
        
        Department? parentDepartment = null;
        if (command.Dto.ParentId is not null)
        {
            parentDepartment = await _departmentsRepository.GetByIdAsync(new DepartmentId(command.Dto.ParentId.Value), cancellationToken);
            if (parentDepartment is null)
            {
                return DepartmentErrors.NotFound(command.Dto.ParentId.Value);
            }
        }

        var nameResult = DepartmentName.Create(command.Dto.Name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }
        
        var slugResult = Slug.Create(command.Dto.Slug);
        if (slugResult.IsFailure)
        {
            return slugResult.Error;
        }
        
        var department = Department.Create(nameResult.Value, slugResult.Value, parentDepartment);

        var departmentLocations = locations.Select(l => new DepartmentLocation(department.Id, l.Id));
        
        await _departmentsRepository.AddAsync(
            department, 
            departmentLocations, 
            cancellationToken
        );
        
        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailure)
        {
            return saveResult.Error;
        }

        LogDepartmentCreated(department.Id.Value);
        
        return new DepartmentDto(
            department.Id.Value,
            department.Name.Value,
            department.Slug,
            department.Path.Value,
            department.ParentId?.Value
        );
    }
    
    [LoggerMessage(
        LogLevel.Information, 
        "Department created with ID {DepartmentId}")]
    private partial void LogDepartmentCreated(Guid departmentId);
}