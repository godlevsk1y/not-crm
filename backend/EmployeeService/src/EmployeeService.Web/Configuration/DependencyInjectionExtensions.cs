using DirectoryService.Contracts.Communication.Http;
using EmployeeService.Core;
using EmployeeService.Infrastructure.Postgres;
using EmployeeService.Web.Documentation;
using EmployeeService.Web.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Exceptions;

namespace EmployeeService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddWebDependencies(configuration)
            .AddCore()
            .AddPostgresInfrastructure(configuration)
            .AddDirectoryService(configuration);

    private static IServiceCollection AddWebDependencies(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddOpenApi(ApiDocumentation.Configure)
            .AddSerilogLogging(configuration)
            .AddHealthCheckEndpoints();

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

    private static IServiceCollection AddHealthCheckEndpoints(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(
                name: "self",
                check: () => HealthCheckResult.Healthy(),
                tags: ["live"])
            .AddDbContextCheck<EmployeeServiceDbContext>(
                name: "database",
                tags: ["ready"],
                customTestQuery: async (context, cancellationToken) =>
                    await context.Database.CanConnectAsync(cancellationToken)
            );

        return services;
    }
}
