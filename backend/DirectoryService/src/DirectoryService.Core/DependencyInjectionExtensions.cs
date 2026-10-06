using Microsoft.Extensions.DependencyInjection;
using Shared.Core.Extensions;

namespace DirectoryService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services) =>
        services.AddHandlersAndValidators(typeof(DependencyInjectionExtensions).Assembly);
}