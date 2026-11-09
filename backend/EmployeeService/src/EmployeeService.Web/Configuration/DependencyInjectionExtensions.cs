using System.Globalization;
using Serilog;

namespace EmployeeService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services)
    {
        services.AddSerilogLogging();
        services.AddControllers();
        services.AddOpenApi();
        services.AddHealthChecks();

        return services;
    }

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        services.AddSerilog(configuration => configuration
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ServiceName", "EmployeeService")
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} {Message:lj}{NewLine}{Exception}",
                formatProvider: CultureInfo.InvariantCulture));

        return services;
    }
}
