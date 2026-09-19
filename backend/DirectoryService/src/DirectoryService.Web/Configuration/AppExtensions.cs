using DirectoryService.Web.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

namespace DirectoryService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseExceptionMiddleware();
        app.UseSerilogRequestLogging();

        if (!app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapControllers();
        app.ConfigureHealthChecks();
        
        return app;
    }

    private static WebApplication ConfigureHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions()
        {
            Predicate = check => check.Tags.Contains("live"),
        });
        
        app.MapHealthChecks("/health/ready", new HealthCheckOptions()
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

        return app;
    }
}