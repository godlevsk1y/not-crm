using System.Diagnostics;
using EmployeeService.Web.Endpoints;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace EmployeeService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.ConfigureLogging();

        if (!app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapGet("/", () => "Hello World!");
        app.MapEmployeeEndpoints();
        app.ConfigureHealthChecks();

        return app;
    }

    private static WebApplication ConfigureLogging(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            using var traceId = LogContext.PushProperty(
                "TraceId", Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
            await next(context);
        });

        app.UseSerilogRequestLogging(options =>
        {
            // Use the host logger and its sinks instead of the static bootstrap logger.
            options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
            options.GetLevel = (context, _, exception) =>
            {
                if (exception is not null || context.Response.StatusCode >= 500)
                {
                    return LogEventLevel.Error;
                }

                return context.Response.StatusCode < 400 &&
                       (context.Request.Path == "/health/live" || context.Request.Path == "/health/ready")
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information;
            };
        });

        return app;
    }

    private static WebApplication ConfigureHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
        });

        return app;
    }
}
