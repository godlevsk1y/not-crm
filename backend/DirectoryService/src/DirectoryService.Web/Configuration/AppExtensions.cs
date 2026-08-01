using DirectoryService.Web.Middlewares;
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
        app.MapHealthChecks("/api/health");
        
        return app;
    }
}