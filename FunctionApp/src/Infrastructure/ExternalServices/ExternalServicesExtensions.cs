using Application.Common.Abstractions.Factories;
using Application.Common.Abstractions.Providers;
using Application.References;
using Application.UserDetails;

using Infrastructure.ExternalServices.FlurlHttp;
using Infrastructure.ExternalServices.Genesys.Auth;
using Infrastructure.ExternalServices.Genesys.Clients;
using Infrastructure.ExternalServices.Genesys.Providers;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.ExternalServices;

public static class ExternalServicesExtensions
{
    public static void AddExternalServices(this IServiceCollection services)
    {
        // 1. Register the Generic HTTP Engine
        services.AddScoped<IFlurlHttpClientFactory, FlurlHttpClientFactory>();

        // 2. Register Genesys Token Management
        // Scoped: This ensures the Provider can access the Scoped ILobContext
        // to use the correct LOB-specific credentials and cache keys.
        services.AddScoped<GenesysTokenClient>();
        services.AddScoped<ITokenProvider, GenesysTokenProvider>();

        // 3. Register Genesys API Clients
        services.AddScoped<IReferencesClient, ReferencesClient>();
        services.AddScoped<IUserDetailsClient, UserDetailsClient>();

        services.AddScoped<IHitCountProviderFactory, HitCountProviderFactory>();
        services.AddScoped<UserDetailsHitCountProvider>();
    }
}
