namespace DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup;

public record DatabaseCleanupOptions
{
    public int BatchSize { get; init; } = 1000;
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(10);
    public int RetentionDays { get; init; } = 30;
}