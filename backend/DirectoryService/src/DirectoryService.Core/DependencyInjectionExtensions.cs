using DirectoryService.Core.Features.Departments.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Extensions;

namespace DirectoryService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (redisConnectionString is not null)
        {
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = redisConnectionString);
        }

        services.AddHybridCache(options =>
            configuration.GetSection("HybridCache").Bind(options));

        services.AddSingleton<DepartmentChildrenCache>();

        services.AddHandlersAndValidators(typeof(DependencyInjectionExtensions).Assembly);

        return services;
    }
}
