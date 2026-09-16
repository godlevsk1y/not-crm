using DirectoryService.Core.Database;
using DirectoryService.Core.Features.Departments;
using DirectoryService.Core.Features.Locations;
using DirectoryService.Core.Features.Positions;
using DirectoryService.Infrastructure.Postgres.Repositories;
using DirectoryService.Infrastructure.Postgres.Services.DatabaseCleanup;
using DirectoryService.Infrastructure.Postgres.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(nameof(DirectoryServiceDbContext));

        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            return builder.Build();
        });
        
        services.AddDbContext<DirectoryServiceDbContext>((sp, options) => 
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
        );
        
        services.AddDbContext<IReadDbContext, DirectoryServiceDbContext>((sp, options) => 
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
        );

        services.AddScoped<ILocationsRepository, LocationsRepository>();
        services.AddScoped<IDepartmentsRepository, DepartmentsRepository>();
        services.AddScoped<IPositionsRepository, PositionsRepository>();

        services.AddScoped<ITransactionManager, TransactionManager>();

        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        
        services.Configure<DatabaseCleanupOptions>(
            configuration.GetSection(nameof(DatabaseCleanupOptions))
        );

        services.AddScoped<DatabaseCleanupService>();
        services.AddHostedService<DatabaseCleanupBackgroundService>();
        
        return services;
    }
}
