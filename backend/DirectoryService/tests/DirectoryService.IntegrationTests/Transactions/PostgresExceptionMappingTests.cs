using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Ids;
using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Shared.Errors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests.Transactions;

public class PostgresExceptionMappingTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;

    public PostgresExceptionMappingTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveChanges_ShouldMapLocationNameUniqueViolationToConflict()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        dbContext.Locations.AddRange(
            CreateLocation("Moscow Office"),
            CreateLocation("Moscow Office"));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(result, ErrorType.Conflict, "location.exists");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentLocationUniqueViolationToConflict()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var department = CreateDepartment();
        var location = CreateLocation("Moscow Office");
        dbContext.AddRange(department, location);
        await dbContext.SaveChangesAsync();
        dbContext.DepartmentLocations.AddRange(
            new DepartmentLocation(department.Id, location.Id),
            new DepartmentLocation(department.Id, location.Id));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(
            result,
            ErrorType.Conflict,
            "department.location.already.added");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentPositionUniqueViolationToConflict()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var department = CreateDepartment();
        var position = CreatePosition();
        dbContext.AddRange(department, position);
        await dbContext.SaveChangesAsync();
        dbContext.DepartmentPositions.AddRange(
            new DepartmentPosition(department.Id, position.Id),
            new DepartmentPosition(department.Id, position.Id));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(
            result,
            ErrorType.Conflict,
            "department.position.already.added");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentParentForeignKeyViolationToNotFound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var department = CreateDepartment();
        dbContext.Departments.Add(department);
        dbContext.Entry(department).Property(d => d.ParentId).CurrentValue =
            new DepartmentId(Guid.NewGuid());

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(result, ErrorType.NotFound, "department.not.found");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentLocationDepartmentForeignKeyViolationToNotFound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var location = CreateLocation("Moscow Office");
        dbContext.Locations.Add(location);
        await dbContext.SaveChangesAsync();
        dbContext.DepartmentLocations.Add(
            new DepartmentLocation(new DepartmentId(Guid.NewGuid()), location.Id));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(result, ErrorType.NotFound, "department.not.found");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentLocationLocationForeignKeyViolationToNotFound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var department = CreateDepartment();
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();
        dbContext.DepartmentLocations.Add(
            new DepartmentLocation(department.Id, new LocationId(Guid.NewGuid())));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(result, ErrorType.NotFound, "location.not.found");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentPositionDepartmentForeignKeyViolationToNotFound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var position = CreatePosition();
        dbContext.Positions.Add(position);
        await dbContext.SaveChangesAsync();
        dbContext.DepartmentPositions.Add(
            new DepartmentPosition(new DepartmentId(Guid.NewGuid()), position.Id));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(result, ErrorType.NotFound, "department.not.found");
    }

    [Fact]
    public async Task SaveChanges_ShouldMapDepartmentPositionPositionForeignKeyViolationToNotFound()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
        var department = CreateDepartment();
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();
        dbContext.DepartmentPositions.Add(
            new DepartmentPosition(department.Id, new PositionId(Guid.NewGuid())));

        var result = await transactionManager.SaveChangesAsync(CancellationToken.None);

        AssertMappedError(result, ErrorType.NotFound, "position.not.found");
    }

    [Fact]
    public async Task Commit_ShouldMapDeferredForeignKeyViolationToNotFound()
    {
        await SetDepartmentParentConstraintDeferredAsync();

        try
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            var transactionManager = scope.ServiceProvider.GetRequiredService<ITransactionManager>();
            var beginResult = await transactionManager.BeginTransactionAsync(CancellationToken.None);
            Assert.True(beginResult.IsSuccess);
            await using var transaction = beginResult.Value;
            var department = CreateDepartment();
            dbContext.Departments.Add(department);
            dbContext.Entry(department).Property(d => d.ParentId).CurrentValue =
                new DepartmentId(Guid.NewGuid());
            var saveResult = await transactionManager.SaveChangesAsync(CancellationToken.None);
            Assert.True(saveResult.IsSuccess);

            var result = await transaction.CommitAsync(CancellationToken.None);

            AssertMappedError(result, ErrorType.NotFound, "department.not.found");
        }
        finally
        {
            await RestoreDepartmentParentConstraintAsync();
        }
    }

    private static Department CreateDepartment() =>
        Department.Create(
            DepartmentName.Create("Product Team").Value,
            Slug.Create("product-team").Value);

    private static Location CreateLocation(string name) =>
        Location.Create(
            LocationName.Create(name).Value,
            Address.Create(
                "Russia",
                "Moscow",
                "Moscow",
                district: null,
                "Tverskaya Street",
                "1",
                "125009").Value);

    private static Position CreatePosition() =>
        Position.Create(PositionName.Create("Developer").Value);

    private static void AssertMappedError(
        UnitResult<Error> result,
        ErrorType expectedType,
        string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedType, result.Error.Type);
        var message = Assert.Single(result.Error.Messages);
        Assert.Equal(expectedCode, message.Code);
    }

    private async Task SetDepartmentParentConstraintDeferredAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE departments
            ALTER CONSTRAINT fk_departments_parent
            DEFERRABLE INITIALLY DEFERRED;
            """);
    }

    private async Task RestoreDepartmentParentConstraintAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE departments
            ALTER CONSTRAINT fk_departments_parent
            NOT DEFERRABLE;
            """);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
