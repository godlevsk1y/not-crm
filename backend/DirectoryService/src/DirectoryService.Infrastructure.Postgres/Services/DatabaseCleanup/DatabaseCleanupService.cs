using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup;

public class DatabaseCleanupService
{
    private readonly DirectoryServiceDbContext _dbContext;
    private readonly DatabaseCleanupOptions _options;

    public DatabaseCleanupService(
        DirectoryServiceDbContext dbContext,
        IOptions<DatabaseCleanupOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<int> CleanupAsync(CancellationToken cancellationToken)
    {
        var threshold = DateTime.UtcNow.AddDays(-_options.RetentionDays);

        var rowsAffected = 0;
        
        rowsAffected += await CleanupPositions(threshold, cancellationToken);
        
        rowsAffected += await CleanupLocations(threshold, cancellationToken);
        
        rowsAffected += await CleanupDepartments(threshold, cancellationToken);
            
        return rowsAffected;
    }

    private async Task<int> CleanupPositions(DateTime threshold, CancellationToken cancellationToken)
    {
        var departmentPositions = _dbContext.DepartmentPositions.IgnoreQueryFilters();
        
        return await _dbContext.Positions
            .IgnoreQueryFilters()
            .Where(p => p.DeletedAt != null && p.DeletedAt < threshold)
            .Where(p => !departmentPositions.Any(dp => dp.PositionId == p.Id))
            .OrderBy(p => p.DeletedAt)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> CleanupLocations(DateTime threshold, CancellationToken cancellationToken)
    {
        var departmentLocations = _dbContext.DepartmentLocations.IgnoreQueryFilters();
        
        return await _dbContext.Locations
            .IgnoreQueryFilters()
            .Where(l => l.DeletedAt != null && l.DeletedAt < threshold)
            .Where(l => !departmentLocations.Any(dl => dl.LocationId == l.Id))
            .OrderBy(l => l.DeletedAt)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<int> CleanupDepartments(DateTime threshold, CancellationToken cancellationToken)
    {
        var allDepartments = _dbContext.Departments.IgnoreQueryFilters();

        return await allDepartments
            .Where(d => d.DeletedAt != null && d.DeletedAt < threshold)
            .Where(d => !allDepartments.Any(child => child.ParentId == d.Id))
            .OrderBy(d => d.DeletedAt)
            .Take(_options.BatchSize)
            .ExecuteDeleteAsync(cancellationToken);
    }
}