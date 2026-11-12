using EmployeeService.Domain.Ids;
using EmployeeService.Domain.Models;
using EmployeeService.Domain.ValueObjects;
using EmployeeService.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Testcontainers.PostgreSql;

namespace EmployeeService.IntegrationTests;

public sealed class EmployeePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task Migrations_CanRollbackAndReapply()
    {
        await using var context = CreateContext();
        context.Database.HasPendingModelChanges().Should().BeFalse();
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("0");
        (await context.Database.GetAppliedMigrationsAsync()).Should().BeEmpty();
        await context.Database.MigrateAsync();
        (await context.Database.GetAppliedMigrationsAsync()).Should().ContainSingle();
        (await context.Employees.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Employee_RoundTripsAndPersistsLifecycleChanges()
    {
        var employee = Employee.Create(EmployeeName.Create("Иван", "Петров").Value,
            new PositionId(Guid.NewGuid()), new DepartmentId(Guid.NewGuid()), new DateOnly(2024, 1, 15)).Value;
        await using (var context = CreateContext())
        {
            context.Employees.Add(employee);
            await context.SaveChangesAsync();
        }

        await AssertStoredEmployee(employee);

        await using (var context = CreateContext())
        {
            var loaded = await context.Employees.SingleAsync(e => e.Id == employee.Id);
            loaded.Rename(EmployeeName.Create("Анна", "Иванова").Value).IsSuccess.Should().BeTrue();
            loaded.ChangeAssignment(new PositionId(Guid.NewGuid()), new DepartmentId(Guid.NewGuid()))
                .IsSuccess.Should().BeTrue();
            loaded.Dismiss(new DateOnly(2025, 3, 20)).IsSuccess.Should().BeTrue();
            employee = loaded;
            await context.SaveChangesAsync();
        }

        await AssertStoredEmployee(employee);

        await using (var context = CreateContext())
        {
            var loaded = await context.Employees.SingleAsync(e => e.Id == employee.Id);
            loaded.SoftDelete();
            employee = loaded;
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            (await context.Employees.AnyAsync()).Should().BeFalse();
            var loaded = await context.Employees.IgnoreQueryFilters().SingleAsync(e => e.Id == employee.Id);
            AssertEmployee(loaded, employee);
            loaded.Restore();
            employee = loaded;
            await context.SaveChangesAsync();
        }

        await AssertStoredEmployee(employee);
    }

    private EmployeeServiceDbContext CreateContext() => new(
        new DbContextOptionsBuilder<EmployeeServiceDbContext>()
            .UseNpgsql(_database.GetConnectionString()).Options);

    private async Task AssertStoredEmployee(Employee expected)
    {
        await using var context = CreateContext();
        var actual = await context.Employees.SingleAsync(e => e.Id == expected.Id);
        AssertEmployee(actual, expected);
    }

    private static void AssertEmployee(Employee actual, Employee expected)
    {
        actual.Id.Should().Be(expected.Id);
        actual.Name.Should().Be(expected.Name);
        actual.PositionId.Should().Be(expected.PositionId);
        actual.DepartmentId.Should().Be(expected.DepartmentId);
        actual.HiredAt.Should().Be(expected.HiredAt);
        actual.DismissedAt.Should().Be(expected.DismissedAt);
        // PostgreSQL timestamps have microsecond precision.
        actual.UpdatedAt.Should().BeCloseTo(expected.UpdatedAt, TimeSpan.FromMicroseconds(1));
        actual.UpdatedAt.Kind.Should().Be(DateTimeKind.Utc);
        if (expected.DeletedAt is { } deletedAt)
        {
            actual.DeletedAt.Should().NotBeNull();
            actual.DeletedAt!.Value.Should().BeCloseTo(deletedAt, TimeSpan.FromMicroseconds(1));
            actual.DeletedAt.Value.Kind.Should().Be(DateTimeKind.Utc);
        }
        else
        {
            actual.DeletedAt.Should().BeNull();
        }
    }
}
