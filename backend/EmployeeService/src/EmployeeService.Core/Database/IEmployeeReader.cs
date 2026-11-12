using EmployeeService.Contracts.Employees;

namespace EmployeeService.Core.Database;

public interface IEmployeeReader
{
    Task<EmployeeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
