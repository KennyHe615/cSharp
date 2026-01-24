using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
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
    }
}
