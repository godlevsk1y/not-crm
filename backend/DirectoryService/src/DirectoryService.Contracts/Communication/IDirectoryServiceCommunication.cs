using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using Shared.Kernel.Errors;

namespace DirectoryService.Contracts.Communication;

public interface IDirectoryServiceCommunication
{
    Task<Result<DepartmentDto, Error>> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken);
}