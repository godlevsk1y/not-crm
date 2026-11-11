using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Infrastructure.Postgres;

public sealed class EmployeeServiceDbContext : DbContext
{
    public EmployeeServiceDbContext(DbContextOptions<EmployeeServiceDbContext> options)
        : base(options) { }
}
