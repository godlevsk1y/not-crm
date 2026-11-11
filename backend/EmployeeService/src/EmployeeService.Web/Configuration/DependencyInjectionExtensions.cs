using EmployeeService.Web.Logging;
using Serilog;
using Serilog.Exceptions;

namespace EmployeeService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration) =>
        services.AddSerilogLogging(configuration);

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .Enrich.With<SourceNameEnricher>()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "EmployeeService"),
            preserveStaticLogger: true
        );

        return services;
    }
}
