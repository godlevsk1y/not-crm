using System.Net;
using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using EmployeeService.Domain.ValueObjects;
using EmployeeService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeService.IntegrationTests;

public class InfrastructureTests(EmployeeServiceTestWebFactory factory)
    : IClassFixture<EmployeeServiceTestWebFactory>, IAsyncLifetime
{
    public Task InitializeAsync() => factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Readiness_ShouldReturnOk_WhenContainerDatabaseIsRunning()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetDatabase_ShouldRemoveEmployeesAndPreserveMigrations_WhenReused()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EmployeeServiceDbContext>();
        var migrations = (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.NotEmpty(migrations);
        Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());

        for (var iteration = 0; iteration < 2; iteration++)
        {
            var employee = Employee.Create(
                EmployeeName.Create("Ada", "Lovelace").Value,
                new PositionId(Guid.NewGuid()),
                new DepartmentId(Guid.NewGuid()),
                new DateOnly(2026, 1, 1)).Value;
            employee.SoftDelete();
            dbContext.Employees.Add(employee);
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
            Assert.Single(await dbContext.Employees.IgnoreQueryFilters().ToListAsync());

            await factory.ResetDatabaseAsync();

            Assert.Empty(await dbContext.Employees.IgnoreQueryFilters().ToListAsync());
            Assert.Equal(migrations, await dbContext.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await dbContext.Database.GetPendingMigrationsAsync());
        }
    }
}
