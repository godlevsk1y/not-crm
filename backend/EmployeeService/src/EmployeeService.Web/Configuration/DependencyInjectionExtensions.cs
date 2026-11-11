using EmployeeService.Web.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Exceptions;

namespace EmployeeService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddSerilogLogging(configuration)
            .AddHealthCheckEndpoints(configuration);

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

    private static IServiceCollection AddHealthCheckEndpoints(this IServiceCollection services,
        IConfiguration configuration)
    {
        var seqUrl = configuration["Seq:ServerUrl"] ??
                     throw new InvalidOperationException("Seq:ServerUrl is not configured.");

        services.AddHealthChecks()
            .AddCheck(
                name: "self",
                check: () => HealthCheckResult.Healthy(),
                tags: ["live"])
            .AddUrlGroup(
                uri: new Uri($"{seqUrl.TrimEnd('/')}/health"),
                name: "seq",
                tags: ["ready"],
                timeout: TimeSpan.FromSeconds(5)
            );

        return services;
    }
}
