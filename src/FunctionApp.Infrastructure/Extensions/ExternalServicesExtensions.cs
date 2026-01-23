using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.ExternalServices.Genesys;
using FunctionApp.Infrastructure.ExternalServices.Genesys.Shared;
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

        // 2. Register Genesys Token Management (Singletons for shared caching)
        services.AddSingleton<GenesysTokenClient>();
        services.AddSingleton<ITokenProvider, GenesysTokenProvider>();

        // 3. Register Specialized API Clients (Scoped)
        // Since GenesysApiClient inherits from FlurlHttpClient, we register it as a concrete type.
        services.AddScoped<GenesysApiClient>();

        services.AddScoped<IGenesysService, GenesysService>();

        // Note: We don't register 'IFlurlHttpClient' directly.
        // Consumers should either use 'GenesysApiClient' or 'IFlurlHttpClientFactory'
        // to ensure they get a properly configured instance.
    }
}
