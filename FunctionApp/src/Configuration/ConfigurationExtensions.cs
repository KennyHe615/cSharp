using System.ComponentModel.DataAnnotations;

using Configuration.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Configuration;

public static class ConfigurationExtensions
{
    public static void AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApplicationInsightsOptions>()
                .Bind(configuration.GetSection(ApplicationInsightsOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<FlurlClientOptions>()
                .Bind(configuration.GetSection(FlurlClientOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<KeyVaultsOptions>()
                .Bind(configuration.GetSection(KeyVaultsOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<GenesysOptions>()
                .Bind(configuration.GetSection(GenesysOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddOptions<LobContextOptions>()
                .Bind(configuration.GetSection(LobContextOptions.SectionName))
                .Validate(options =>
                          {
                              foreach (KeyValuePair<string, LobSettings> kvp in options)
                              {
                                  ValidationContext ctx = new(kvp.Value);
                                  Validator.ValidateObject(kvp.Value, ctx, true);
                              }

                              return true;
                          },
                          "One or more LobContext entries are invalid.")
                .ValidateOnStart();
    }
}
