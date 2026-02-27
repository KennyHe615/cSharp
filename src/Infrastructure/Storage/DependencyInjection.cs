using Infrastructure.Storage.Blob;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Storage;

public static class DependencyInjection
{
    public static void AddStorage(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BlobOptions>().Bind(configuration.GetSection("BlobStorage"));

        services.AddSingleton<AzureBlobStorage>();
    }
}
