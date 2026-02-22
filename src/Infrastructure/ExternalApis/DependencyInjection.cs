using Infrastructure.ExternalApis.Http;
using Infrastructure.ExternalApis.Http.Policies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.ExternalApis;

public static class DependencyInjection
{
    public static IServiceCollection AddExternalApis(this IServiceCollection services, IConfiguration configuration)
    {
        services
           .AddOptions<HttpClientResilienceOptions>()
           .Bind(configuration.GetSection(HttpClientResilienceOptions.SectionName))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        services.AddSingleton<IHttpResiliencePolicyFactory, HttpResiliencePolicyFactory>();
        services.AddSingleton<IHttpApiClientFactory, HttpApiClientFactory>();

        return services;
    }
}
