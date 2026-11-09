using Scalar.AspNetCore;
using Shared.Framework.Middlewares;

namespace EmployeeService.Web.Configuration;

public static class AppExtensions
{
    public static IApplicationBuilder Configure(this WebApplication app)
    {
        app.UseExceptionMiddleware();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapControllers();
        app.MapHealthChecks("/health/live");

        return app;
    }
}
