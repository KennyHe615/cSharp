using Application.Shared.Context;

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

        // services.AddKeyVaultSecretProvider();

        services.AddPersistence();

        services.AddScoped<ILobContextAccessor, LobContextAccessor>();
        services.AddScoped<ILobContext, LobContext>();

        // services.AddBlobStorageClient();
        // services.AddRepositories();
    }
}
