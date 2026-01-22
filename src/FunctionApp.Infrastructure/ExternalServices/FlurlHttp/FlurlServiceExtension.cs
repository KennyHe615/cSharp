using FunctionApp.Infrastructure.ExternalServices.Genesys;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public static class FlurlServiceExtension
{
    public static void AddFlurlHttpClient(this IServiceCollection services)
    {
        services.AddSingleton<TokenHttpClient>();

        services.AddSingleton<ITokenProvider, GenesysTokenProvider>();

        services.AddScoped<IHttpClient, HttpClient>();
    }
}
