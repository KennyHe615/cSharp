using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Configuration;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Explicitly bind all options sections to strongly-typed options.
    /// Add new options here as needed.
    /// </summary>
    public static void AddFunctionAppConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApplicationInsightsOptions>()
                .Bind(config: configuration.GetSection(ApplicationInsightsOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<FlurlClientOptions>()
                .Bind(config: configuration.GetSection(FlurlClientOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<GenesysOptions>()
                .Bind(config: configuration.GetSection(GenesysOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();
    }
}
