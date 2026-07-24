
using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Locations;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using DepartmentsService = DirectoryService.Core.Features.Departments.DepartmentsService;
using LocationsService = DirectoryService.Core.Features.Locations.LocationsService;

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