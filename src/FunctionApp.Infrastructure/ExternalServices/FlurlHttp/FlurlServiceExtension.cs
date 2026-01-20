using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public static class FlurlServiceExtension
{
    public static void AddFlurlHttpClient(this IServiceCollection services)
    {
        services.AddScoped<IHttpClient, HttpClient>();
    }
}
