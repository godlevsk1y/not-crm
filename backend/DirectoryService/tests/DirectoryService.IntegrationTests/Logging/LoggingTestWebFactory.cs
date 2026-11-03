using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Core;

namespace DirectoryService.IntegrationTests.Logging;

public class LoggingTestWebFactory : DirectoryServiceTestWebFactory
{
    public LogCapture Logs { get; } = new();

    public bool Unhealthy { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<ILogEventSink>(Logs);
            services.AddControllers().AddApplicationPart(typeof(LoggingTestController).Assembly);
            services.AddHealthChecks().AddCheck(
                "logging-test",
                () => Unhealthy ? HealthCheckResult.Unhealthy("Test failure") : HealthCheckResult.Healthy(),
                tags: ["live"]);
        });
    }
}
