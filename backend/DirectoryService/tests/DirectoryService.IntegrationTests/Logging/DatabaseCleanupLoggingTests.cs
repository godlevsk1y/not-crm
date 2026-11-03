using DirectoryService.Domain.Models;
using DirectoryService.Domain.ValueObjects;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;

namespace DirectoryService.IntegrationTests.Logging;

public class DatabaseCleanupLoggingTests : IClassFixture<DirectoryServiceTestWebFactory>, IAsyncLifetime
{
    private readonly DirectoryServiceTestWebFactory _factory;

    public DatabaseCleanupLoggingTests(DirectoryServiceTestWebFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData(false, LogEventLevel.Debug)]
    [InlineData(true, LogEventLevel.Information)]
    public async Task Cleanup_ShouldLogOnlyDeletionsAtInformation(bool hasExpiredPosition, LogEventLevel expectedLevel)
    {
        if (hasExpiredPosition)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<DirectoryServiceDbContext>();
            var position = Position.Create(PositionName.Create("Expired position").Value);
            context.Positions.Add(position);
            context.Entry(position).Property(p => p.DeletedAt).CurrentValue = DateTime.UtcNow.AddDays(-31);
            await context.SaveChangesAsync();
        }

        var capture = new LogCapture();
        await using var logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(capture).CreateLogger();
        using var loggerFactory = new LoggerFactory();
        loggerFactory.AddSerilog(logger);
        using var service = new DatabaseCleanupBackgroundService(
            _factory.Services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DatabaseCleanupOptions { Interval = TimeSpan.FromMilliseconds(20) }),
            loggerFactory.CreateLogger<DatabaseCleanupBackgroundService>());

        await service.StartAsync(CancellationToken.None);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (capture.Events.Count == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
            }
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        var completed = capture.Events[0];
        Assert.Equal(expectedLevel, completed.Level);
        Assert.Null(completed.Exception);
        if (hasExpiredPosition)
        {
            Assert.Equal(1, Assert.IsType<ScalarValue>(completed.Properties["RowsAffected"]).Value);
        }
        else
        {
            Assert.DoesNotContain(capture.Events, e => e.Level >= LogEventLevel.Information);
        }
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _factory.ResetDatabaseAsync();
}
