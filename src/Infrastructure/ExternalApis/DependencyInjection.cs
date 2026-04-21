using Application.Abstractions.External;
using Application.Abstractions.Planning;
using Application.Abstractions.Recovery;
using Application.DTOs.Planning;

using Infrastructure.ExternalApis.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Auth;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.References;
using Infrastructure.ExternalApis.Providers.Genesys.Transport;
using Infrastructure.ExternalApis.Shared.Http;
using Infrastructure.ExternalApis.Shared.Policies;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.ExternalApis;

public static class DependencyInjection
{
    public static void AddExternalApis(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<HttpClientResilienceOptions>()
                .Bind(configuration.GetSection(HttpClientResilienceOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<GenesysOptions>()
                .Bind(configuration.GetSection(GenesysOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.PostConfigure<GenesysOptions>(options =>
                                               {
                                                   PlannedIntervalDto.ConfigurePageSize(options.DefaultPageSize);
                                               });

        services.AddMemoryCache();

        services.AddSingleton<IHttpResiliencePolicyFactory, HttpResiliencePolicyFactory>();
        services.AddSingleton<IHttpApiClientFactory, HttpApiClientFactory>();

        services.AddScoped<IGenesysTokenApiClient, GenesysTokenApiClient>();
        services.AddScoped<IGenesysTokenStore, GenesysTokenStore>();
        services.AddScoped<IGenesysTokenProvider, GenesysTokenProvider>();
        services.AddScoped<IGenesysApiClient, GenesysApiClient>();
        services.AddScoped<IRecoveryIntervalPolicy, GenesysRecoveryIntervalPolicy>();

        services.AddScoped<IAnalyticsUsersDetailsClient, UsersDetailsClient>();
        services.AddScoped<IReferenceApiClient, ReferencesClient>();

        services.AddScoped<IIntervalPlanner, IntervalPlanner>();
        services.AddScoped<IHitCountProviderFactory, HitCountProviderFactory>();
        services.AddScoped<UsersDetailsHitCountProvider>();
        services.AddScoped<ConversationsDetailsHitCountProvider>();
    }
}
