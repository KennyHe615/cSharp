using FunctionApp.Infrastructure.KeyVault;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddApplicationInsights();

        services.AddMemoryCache();

        services.AddExternalServices();

        services.AddKeyVaultSecretProvider();

        services.AddPersistence();

        // services.AddBlobStorageClient();
        // services.AddRepositories();
    }
}
