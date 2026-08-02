using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Domain.Models;
using DirectoryService.Shared.Errors;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Features.Departments.AddLocation;

public partial class AddLocationHandler : ICommandHandler<AddLocationCommand>
{
    private readonly IDepartmentsRepository _departmentsRepository;
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILogger<AddLocationHandler> _logger;

    public AddLocationHandler(
        IDepartmentsRepository departmentsRepository,
        ILocationsRepository locationsRepository,
        ILogger<AddLocationHandler> logger)
    {
        _departmentsRepository = departmentsRepository;
        _locationsRepository = locationsRepository;
        _logger = logger;
    }
    
    public async Task<UnitResult<Error>> Handle(AddLocationCommand command, CancellationToken cancellationToken)
    {
        var department = await _departmentsRepository.GetByIdAsync(command.DepartmentId, cancellationToken);

        if (department is null)
        {
            return DepartmentErrors.NotFound(command.DepartmentId);
        }
        
        var location = await _locationsRepository.GetByIdAsync(command.LocationId, cancellationToken);
        if (location is null)
        {
            return LocationErrors.NotFound(command.LocationId);
        }

        var departmentLocation = new DepartmentLocation(department.Id, location.Id);

        if (await _departmentsRepository.HasDepartmentLocationAsync(departmentLocation, cancellationToken))
        {
            return DepartmentErrors.LocationAlreadyAdded(command.DepartmentId, command.LocationId);
        }
        
        await _departmentsRepository.AddLocationAsync(departmentLocation, cancellationToken);
        
        LogLocationAdded(location.Id.Value, department.Id.Value);
        
        return UnitResult.Success<Error>();
    }
    
    [LoggerMessage(
        LogLevel.Information, 
        "Location {LocationId} added to department {DepartmentId}")]
    private partial void LogLocationAdded(Guid locationId, Guid departmentId);
}