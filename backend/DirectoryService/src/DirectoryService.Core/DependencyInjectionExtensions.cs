using DirectoryService.Core.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjectionExtensions).Assembly;
        
        services.AddValidatorsFromAssembly(assembly);

        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes
                .AssignableToAny(
                    typeof(ICommandHandler<,>),
                    typeof(ICommandHandler<>),
                    typeof(IQueryHandler<,>),
                    typeof(IQueryHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime()
        );
        
        return services;
    }
}
