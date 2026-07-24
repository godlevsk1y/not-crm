using DirectoryService.Core.Departments;
using DirectoryService.Core.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjectionExtensions).Assembly);
        
        services.AddScoped<ILocationsService, LocationsService>();
        services.AddScoped<IDepartmentsService, DepartmentsService>();
        
        return services;
    }
}