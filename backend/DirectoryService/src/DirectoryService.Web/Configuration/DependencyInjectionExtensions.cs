using DirectoryService.Core;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Shared.Errors;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Exceptions;

namespace DirectoryService.Web.Configuration;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddWebDependencies(configuration)
            .AddCore()
            .AddPostgresInfrastructure(configuration);
    
    private static IServiceCollection AddWebDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilogLogging(configuration);
        
        services.AddOpenApi();
        services.AddHealthChecks();

        services.AddControllers();
        services.Configure<RouteOptions>(options => 
            options.LowercaseUrls = true
        );

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = 
                context => new BadRequestObjectResult(
                    Envelope.Failure(
                        Error.BadRequest(
                            new ErrorMessage(
                                "invalid.model.state",
                                "The request is provided in the incorrect format."
                            )
                        )
                    )
                );
        });

        return services;
    }

    private static IServiceCollection AddSerilogLogging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((sp, lc) => lc
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(sp)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("ServiceName", "DirectoryService")
        );
        
        return services;
    }
}