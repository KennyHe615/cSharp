using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Extensions;

// TODO: Open services one by one
public static class ExternalServicesExtensions
{
    public static void AddExternalServices(this IServiceCollection services)
    {
        // 1. Register the Generic HTTP Engine
        // services.AddSingleton<IFlurlHttpClientFactory, FlurlHttpClientFactory>();

        // 2. Register Genesys Token Management
        // Scoped: This ensures the Provider can access the Scoped ILobContext
        // to use the correct LOB-specific credentials and cache keys.
        // services.AddScoped<GenesysTokenClient>();
        // services.AddScoped<ITokenProvider, GenesysTokenProvider>();

        // 3. Register Genesys API Clients
        // services.AddScoped<IReferencesClient, ReferencesClient>();
    }
}
