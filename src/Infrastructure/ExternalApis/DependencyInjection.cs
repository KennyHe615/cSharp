using Infrastructure.ExternalApis.Http;
using Infrastructure.ExternalApis.Http.Policies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.ExternalApis;

public static class DependencyInjection
{
    public static void AddExternalApis(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
           .AddOptions<HttpClientResilienceOptions>()
           .Bind(configuration.GetSection(HttpClientResilienceOptions.SectionName))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        services.AddSingleton<IHttpResiliencePolicyFactory, HttpResiliencePolicyFactory>();

        services.AddSingleton<IHttpApiClientFactory, HttpApiClientFactory>();
    }
}
