using DirectoryService.Core;
using DirectoryService.Infrastructure.Postgres;
using DirectoryService.Shared.Errors;
using DirectoryService.Web.Results;
using Microsoft.AspNetCore.Mvc;

namespace DirectoryService.Web;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration) =>
        services
            .AddWebDependencies()
            .AddCore()
            .AddPostgresInfrastructure(configuration);
    
    private static IServiceCollection AddWebDependencies(this IServiceCollection services)
    {
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
}