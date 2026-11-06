using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Extensions;

namespace DirectoryService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddHybridCache();

        services.AddHandlersAndValidators(typeof(DependencyInjectionExtensions).Assembly);

        return services;
    }
}
