using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Core.Extensions;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Shared.Errors;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments;

public partial class DepartmentsService : IDepartmentsService
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ILocationsRepository _locationsRepository;
    
    private readonly IValidator<CreateDepartmentRequest> _createDepartmentRequestValidator;
    private readonly IValidator<UpdateDepartmentRequest> _updateDepartmentRequestValidator;
    private readonly ILogger<DepartmentsService> _logger;

    public DepartmentsService(
        IDepartmentsRepository departmentsRepository,
        ILocationsRepository locationsRepository,
        IValidator<CreateDepartmentRequest> createDepartmentRequestValidator,
        IValidator<UpdateDepartmentRequest> updateDepartmentRequestValidator,
        ILogger<DepartmentsService> logger
    )
    {
        _createDepartmentRequestValidator = createDepartmentRequestValidator;
        _updateDepartmentRequestValidator = updateDepartmentRequestValidator;
        _logger = logger;
        _departmentsRepository = departmentsRepository;
        _locationsRepository = locationsRepository;
    }
    
    public async Task<Result<DepartmentDto, Error>> CreateAsync(CreateDepartmentRequest dto, CancellationToken cancellationToken)
    {
        var validationResult = await _createDepartmentRequestValidator.ValidateAsync(dto, cancellationToken);

        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        List<Location> locations = [];
        foreach (var locationId in dto.LocationIds)
        {
            var location = await _locationsRepository.GetByIdAsync(locationId, cancellationToken);
            if (location is null)
            {
                return LocationErrors.NotFound(locationId);
            }
            
            locations.Add(location);
        }
        
        Department? parentDepartment = null;
        if (dto.ParentId is not null)
        {
            parentDepartment = await _departmentsRepository.GetByIdAsync(dto.ParentId.Value, cancellationToken);
            if (parentDepartment is null)
            {
                return DepartmentErrors.NotFound(dto.ParentId.Value);
            }
        }

        var nameResult = DepartmentName.Create(dto.Name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }
        
        var slugResult = Slug.Create(dto.Slug);
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

        LogDepartmentCreated(department.Id.Value);
        
        return new DepartmentDto(
            department.Id.Value,
            department.Name.Value,
            department.Slug,
            department.Path.Value,
            department.ParentId?.Value
        );
    }

    public async Task<Result<Guid, Error>> UpdateAsync(Guid id, UpdateDepartmentRequest dto, CancellationToken cancellationToken)
    {
        var validationResult = await _updateDepartmentRequestValidator.ValidateAsync(dto, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToError();
        }

        var department = await _departmentsRepository.GetByIdWithParentAsync(id, cancellationToken);
        if (department is null)
        {
            return DepartmentErrors.NotFound(id);
        }

        if (dto.Name is not null)
        {
            var newNameResult = DepartmentName.Create(dto.Name);
            if (newNameResult.IsFailure)
            {
                return newNameResult.Error;
            }
            
            department.Rename(newNameResult.Value);
        }

        if (dto.Slug is not null)
        {
            var slugResult = Slug.Create(dto.Slug);
            if (slugResult.IsFailure)
            {
                return slugResult.Error;
            }
            
            department.ChangeSlug(slugResult.Value);
        }

        if (dto.ParentId == Guid.Empty)
        {
            var setParentResult = department.SetParent(parent: null);
            if (setParentResult.IsFailure)
            {
                return setParentResult.Error;
            }
        }
        else if (dto.ParentId is not null)
        {
            var parentDepartment = await _departmentsRepository.GetByIdAsync(dto.ParentId.Value, cancellationToken);
            if (parentDepartment is null)
            {
                return DepartmentErrors.NotFound(dto.ParentId.Value);
            }
        
            var setParentResult = department.SetParent(parentDepartment);
            if (setParentResult.IsFailure)
            {
                return setParentResult.Error;
            }
        }
        
        await _departmentsRepository.SaveAsync(cancellationToken);
        
        LogDepartmentUpdated(department.Id.Value);
        
        return department.Id.Value;
    }

    public async Task<UnitResult<Error>> AddLocationAsync(Guid departmentId, Guid locationId, CancellationToken cancellationToken)
    {
        var department = await _departmentsRepository.GetByIdAsync(departmentId, cancellationToken);

        if (department is null)
        {
            return DepartmentErrors.NotFound(departmentId);
        }
        
        var location = await _locationsRepository.GetByIdAsync(locationId, cancellationToken);
        if (location is null)
        {
            return LocationErrors.NotFound(locationId);
        }

        var departmentLocation = new DepartmentLocation(department.Id, location.Id);

        if (await _departmentsRepository.HasDepartmentLocationAsync(departmentLocation, cancellationToken))
        {
            return DepartmentErrors.LocationAlreadyAdded(departmentId, locationId);
        }
        
        await _departmentsRepository.AddLocationAsync(departmentLocation, cancellationToken);
        
        LogLocationAdded(location.Id.Value, department.Id.Value);
        
        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> RemoveLocationAsync(Guid departmentId, Guid locationId, CancellationToken cancellationToken)
    {
        var departmentLocation = await _departmentsRepository
            .GetDepartmentLocation(departmentId, locationId, cancellationToken);
        
        if (departmentLocation is null)
        {
            return DepartmentErrors.DepartmentLocationNotFound(departmentId, locationId);
        }
        
        await _departmentsRepository.RemoveLocationAsync(departmentLocation, cancellationToken);
        
        LogLocationRemoved(departmentLocation.LocationId, departmentLocation.DepartmentId);
        
        return UnitResult.Success<Error>();
    }

    
    
    [LoggerMessage(
        LogLevel.Information, 
        "Department created with ID {DepartmentId}")]
    partial void LogDepartmentCreated(Guid departmentId);

    [LoggerMessage(
        LogLevel.Information, 
        "Department updated with ID {DepartmentId}")]
    partial void LogDepartmentUpdated(Guid departmentId);

    [LoggerMessage(
        LogLevel.Information, 
        "Location {LocationId} added to department {DepartmentId}")]
    partial void LogLocationAdded(Guid locationId, Guid departmentId);
    
    [LoggerMessage(
        LogLevel.Information,
        "Location {LocationId} removed from department {DepartmentId}")]
    partial void LogLocationRemoved(Guid locationId, Guid departmentId);
}