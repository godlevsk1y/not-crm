using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DirectoryService.Contracts.Communication.Http;

public static class DirectoryServiceExtensions
{
    public static IServiceCollection AddDirectoryService(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.Configure<DirectoryServiceOptions>(configuration.GetSection(nameof(DirectoryServiceOptions)));

        services.AddHttpClient<IDirectoryServiceCommunication, HttpCommunicationService>(
            (sp, config) =>
            {
                var directoryServiceOptions = sp.GetRequiredService<IOptions<DirectoryServiceOptions>>().Value;

                config.BaseAddress = directoryServiceOptions.BaseAddress;
                config.Timeout = directoryServiceOptions.Timeout;
            }
        );
        
        return services;
    }
}