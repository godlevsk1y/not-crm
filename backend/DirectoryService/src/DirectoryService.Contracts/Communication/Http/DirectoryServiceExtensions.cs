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
        services.AddOptions<DirectoryServiceOptions>()
            .Bind(configuration.GetSection(nameof(DirectoryServiceOptions)))
            .Validate(
                static options =>
                    options.BaseAddress is { IsAbsoluteUri: true, Scheme: "http" or "https" },
                "DirectoryServiceOptions:BaseAddress must be an absolute HTTP(S) URL.")
            .Validate(
                static options =>
                    options.Timeout > TimeSpan.Zero
                    && options.Timeout <= TimeSpan.FromMilliseconds(int.MaxValue),
                "DirectoryServiceOptions:Timeout must be positive and no greater than 2147483647 milliseconds")
            .ValidateOnStart();

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