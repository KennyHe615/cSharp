using Infrastructure.Configuration.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Configuration;

public static class DependencyInjection
{
    public static void AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
           .AddOptions<ApplicationInsightsOptions>()
           .Bind(configuration.GetSection(ApplicationInsightsOptions.SectionName))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        services
           .AddOptions<KeyVaultOptions>()
           .Bind(configuration.GetSection(KeyVaultOptions.SectionName))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        // services
        //    .AddOptions<IntervalSubdivisionOptions>()
        //    .Configure(options =>
        //               {
        //                   options.HighHitThreshold = 200_000;
        //                   options.AdjustmentMinutes = 120;
        //                   options.MinIntervalMinutes = 1;
        //               });
    }
}
