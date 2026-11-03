using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup;

public partial class DatabaseCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DatabaseCleanupOptions _options;
    private readonly ILogger<DatabaseCleanupBackgroundService> _logger;

    public DatabaseCleanupBackgroundService(
        IServiceScopeFactory scopeFactory, 
        IOptions<DatabaseCleanupOptions> options, 
        ILogger<DatabaseCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    var cleanupService = scope.ServiceProvider
                        .GetRequiredService<DatabaseCleanupService>();

                    var rowsAffected = await cleanupService.CleanupAsync(stoppingToken);

                    if (rowsAffected == 0)
                    {
                        LogCleanupEmpty();
                    }
                    else
                    {
                        LogCleanupCompleted(rowsAffected);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    LogBackgroundServiceStopped();
                    break;
                }
                catch (Exception ex)
                {
                    LogUnhandledCleanupException(ex);
                }
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            LogBackgroundServiceStopped();
        }
        catch (Exception ex)
        {
            LogUnhandledCleanupException(ex);
        }
    }

    [LoggerMessage(
        LogLevel.Information, 
        "Database cleanup completed: {RowsAffected} rows deleted")]
    private partial void LogCleanupCompleted(int rowsAffected);

    [LoggerMessage(LogLevel.Debug, "Database cleanup completed: no rows deleted")]
    private partial void LogCleanupEmpty();

    [LoggerMessage(
        LogLevel.Debug,
        "Database cleanup stopped")]
    private partial void LogBackgroundServiceStopped();

    [LoggerMessage(
        LogLevel.Error, 
        "Database cleanup failed")]
    private partial void LogUnhandledCleanupException(Exception exception);
}
