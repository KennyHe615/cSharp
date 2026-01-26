using FunctionApp.Application.References.Clients;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.ExternalServices.Genesys.Clients;
using FunctionApp.Infrastructure.ExternalServices.Genesys.Shared.Token;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.Extensions;

public static class ExternalServicesExtensions
{
    public static void AddExternalServices(this IServiceCollection services)
    {
        // 1. Register the Generic HTTP Engine
        services.AddSingleton<IFlurlHttpClientFactory, FlurlHttpClientFactory>();

        // 2. Register Genesys Token Management
        // Scoped: This ensures the Provider can access the Scoped ILobContext
        // to use the correct LOB-specific credentials and cache keys.
        services.AddScoped<GenesysTokenClient>();
        services.AddScoped<ITokenProvider, GenesysTokenProvider>();

        // 3. Register Genesys API Clients
        services.AddScoped<IReferencesClient, ReferencesClient>();
    }
}
