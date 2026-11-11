using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Shared.Framework.Middlewares;

namespace DirectoryService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
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
                if (exception is not null || context.Response.StatusCode >= 500 ||
                    context.Features.Get<IExceptionHandlerFeature>() is not null)
                {
                    return LogEventLevel.Error;
                }

                return context.Response.StatusCode < 400 &&
                       (context.Request.Path == "/health/live" || context.Request.Path == "/health/ready")
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information;
            };
            options.EnrichDiagnosticContext = (diagnosticContext, context) =>
            {
                diagnosticContext.Set("RequestAborted", context.RequestAborted.IsCancellationRequested);
                if (context.Features.Get<IExceptionHandlerFeature>() is not null)
                {
                    diagnosticContext.Set("RequestFailed", true);
                    diagnosticContext.Set("ResponseStarted", context.Response.HasStarted);
                }
            };
        });
        app.UseExceptionMiddleware();

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
