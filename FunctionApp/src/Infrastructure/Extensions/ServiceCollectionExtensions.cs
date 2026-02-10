using Application.Common.Abstractions.Context;

using Infrastructure.Azure.ApplicationInsights;
using Infrastructure.Azure.KeyVaults;
using Infrastructure.ExternalServices;
using Infrastructure.Shared.Context;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(this IServiceCollection services)
    {
        services.AddApplicationInsights();

        services.AddMemoryCache();

        services.AddExternalServices();

        services.AddKeyVaultsSecretProvider();

        services.AddPersistence();

        services.AddScoped<ILobContextAccessor, LobContextAccessor>();
        services.AddScoped<ILobContext, LobContext>();

        // services.AddBlobStorageClient();
        // services.AddRepositories();
    }
}
