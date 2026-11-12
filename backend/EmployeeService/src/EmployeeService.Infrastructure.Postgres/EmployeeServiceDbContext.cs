using EmployeeService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeService.Infrastructure.Postgres;

public sealed class EmployeeServiceDbContext : DbContext
{
    public EmployeeServiceDbContext(DbContextOptions<EmployeeServiceDbContext> options)
        : base(options) { }

    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmployeeServiceDbContext).Assembly);
    }
}
