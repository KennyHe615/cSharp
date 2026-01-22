using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.ExternalServices.Genesys;
using FunctionApp.Infrastructure.KeyVault;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddApplicationInsights();

        services.AddMemoryCache();

        services.AddFlurlHttpClient();

        services.AddKeyVaultSecretProvider();

        services.AddPersistence();

        services.AddScoped<IGenesysService, GenesysService>();

        // services.AddBlobStorageClient();
        // services.AddRepositories();
    }
}
