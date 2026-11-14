using EmployeeService.Domain.Models;

namespace EmployeeService.Core.Features.Employees;

public interface IEmployeeRepository
{
    Task<Guid> AddAsync(Employee employee, CancellationToken cancellationToken);
}