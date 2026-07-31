using CSharpFunctionalExtensions;
using DirectoryService.Contracts.WebApi.Departments;
using DirectoryService.Shared.Errors;

namespace DirectoryService.Core.Features.Departments;

public interface IDepartmentsService
{
    Task<Result<DepartmentDto, Error>> CreateAsync(CreateDepartmentRequest dto, CancellationToken cancellationToken);
    
    Task<Result<Guid, Error>> UpdateAsync(Guid id, UpdateDepartmentRequest dto, CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> AddLocationAsync(Guid departmentId, Guid locationId, CancellationToken cancellationToken);
    
    Task<UnitResult<Error>> RemoveLocationAsync(Guid departmentId, Guid locationId, CancellationToken cancellationToken);
}