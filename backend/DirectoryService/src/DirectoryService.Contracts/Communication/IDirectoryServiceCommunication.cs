using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Contracts.Positions;
using Shared.Kernel.Errors;

namespace DirectoryService.Contracts.Communication;

public interface IDirectoryServiceCommunication
{
    Task<Result<DepartmentDto, Error>> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken);

    Task<Result<PositionDto, Error>> GetPositionByIdAsync(Guid positionId, CancellationToken cancellationToken);
}
