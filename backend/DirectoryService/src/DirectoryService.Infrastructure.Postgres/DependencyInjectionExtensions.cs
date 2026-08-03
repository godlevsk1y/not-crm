using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Infrastructure.Postgres.Repositories;
using DirectoryService.Infrastructure.Postgres.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(nameof(DirectoryServiceDbContext));

        services.AddDbContext<DirectoryServiceDbContext>(options => 
            options.UseNpgsql(connectionString)
        );

        services.AddScoped<ILocationsRepository, LocationsRepository>();
        services.AddScoped<IDepartmentsRepository, DepartmentsRepository>();

        services.AddScoped<ITransactionManager, TransactionManager>();
        
        return services;
    }
}