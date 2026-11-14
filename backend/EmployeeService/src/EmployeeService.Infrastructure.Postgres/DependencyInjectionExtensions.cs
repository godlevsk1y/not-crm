using EmployeeService.Core.Database;
using EmployeeService.Core.Features.Employees;
using EmployeeService.Infrastructure.Postgres.Repositories;
using EmployeeService.Infrastructure.Postgres.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Core.Database;

namespace EmployeeService.Infrastructure.Postgres;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPostgresInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(nameof(EmployeeServiceDbContext));

        services.AddSingleton<NpgsqlDataSource>(sp =>
        {
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            builder.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            return builder.Build();
        });

        services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddDbContext<EmployeeServiceDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>())
        );

        services.AddRepositories();

        services.AddScoped<ITransactionManager, TransactionManager>();

        return services;
    }

    private static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        return services;
    }
}
