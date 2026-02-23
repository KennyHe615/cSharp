using Application.Abstractions.External;

using Infrastructure.ExternalApis.Genesys;
using Infrastructure.ExternalApis.Genesys.Abstractions;
using Infrastructure.ExternalApis.Genesys.Analytics;
using Infrastructure.ExternalApis.Genesys.References;
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

        services
           .AddOptions<GenesysOptions>()
           .Bind(configuration.GetSection(GenesysOptions.SectionName))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        services.AddMemoryCache();

        services.AddSingleton<IHttpResiliencePolicyFactory, HttpResiliencePolicyFactory>();
        services.AddSingleton<IHttpApiClientFactory, HttpApiClientFactory>();

        services.AddScoped<IGenesysTokenProvider, GenesysTokenProvider>();
        services.AddScoped<IGenesysApiClient, GenesysApiClient>();

        services.AddScoped<IAnalyticsUsersDetailsClient, UsersDetailsClient>();
        services.AddScoped<IReferenceApiClient, ReferencesClient>();
    }
}
