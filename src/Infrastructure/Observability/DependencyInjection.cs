using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Observability;

public static class DependencyInjection
{
    public static void AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Keep host-specific AI wiring in:
        // - AddApplicationInsightsForFunctions(...)
        // - AddApplicationInsightsForWorker(...)
        // from ApplicationInsightsExtensions.
        //
        // This feature module is reserved for shared observability services
        // that are host-agnostic.
    }
}
