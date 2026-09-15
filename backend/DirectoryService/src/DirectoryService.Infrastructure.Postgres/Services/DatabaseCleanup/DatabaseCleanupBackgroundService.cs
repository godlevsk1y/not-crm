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
                await using var scope = _scopeFactory.CreateAsyncScope();

                var cleanupService = scope.ServiceProvider
                    .GetRequiredService<DatabaseCleanupService>();

                var rowsAffected = await cleanupService.CleanupAsync(stoppingToken);

                LogCleanupSucceed(rowsAffected);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            LogBackgroundServiceStopped();
        }
        catch (Exception ex)
        {
            LogUnhandledException(ex);
        }
    }

    [LoggerMessage(
        LogLevel.Information, 
        "Successfully cleaned up {rowsAffected} row(s)")]
    private partial void LogCleanupSucceed(int rowsAffected);

    [LoggerMessage(
        LogLevel.Information, 
        "Background Service is stopped.")]
    private partial void LogBackgroundServiceStopped();

    [LoggerMessage(
        LogLevel.Error, 
        "Database cleanup failed")]
    private partial void LogUnhandledException(Exception exception);
}