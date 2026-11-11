using System.Diagnostics;
using Serilog;
using Serilog.Context;

namespace EmployeeService.Web.Configuration;

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
        });

        app.MapGet("/", () => "Hello World!");

        return app;
    }
}
