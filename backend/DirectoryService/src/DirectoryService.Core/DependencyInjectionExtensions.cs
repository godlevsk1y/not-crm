using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Features.Departments.Queries.GetDepartmentList;
using DirectoryService.Core.Features.Locations.Queries.GetTopLocationWithDepartmentsCount;
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
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(ICommandHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime()
        );
        
        services.AddScoped<GetTopLocationsWithDepartmentCountQueryHandler>();
        services.AddScoped<GetDepartmentListQueryHandler>();
        
        return services;
    }
}