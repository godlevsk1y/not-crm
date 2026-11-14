using EmployeeService.Core.Features.Employees;
using EmployeeService.Domain.Models;

namespace EmployeeService.Infrastructure.Postgres.Repositories;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly EmployeeServiceDbContext _context;

    public EmployeeRepository(EmployeeServiceDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> AddAsync(Employee employee, CancellationToken cancellationToken)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);

        return employee.Id;
    }
}