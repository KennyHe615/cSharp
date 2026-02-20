using Infrastructure.Configuration.Options;

using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Observability;

/// <summary>
/// Application Insights wiring for worker and Azure Functions isolated hosts.
/// </summary>
public static class ApplicationInsightsExtensions
{
    /// <summary>
    /// Adds Application Insight for a Worker Service host.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="appCategory">Logger category prefix for app logs.</param>
    public static void AddApplicationInsightsForWorker(this IServiceCollection services, string appCategory)
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        ConfigureTelemetryOptions(services);
        ConfigureLoggingFilters(services, appCategory);
    }

    /// <summary>
    /// Adds Application Insights for an Azure Functions isolated host.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="appCategory">Logger category prefix for app logs.</param>
    public static void AddApplicationInsightsForFunctions(this IServiceCollection services, string appCategory)
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        ConfigureTelemetryOptions(services);
        services.ConfigureFunctionsApplicationInsights();
        ConfigureLoggingFilters(services, appCategory);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Binds <see cref="ApplicationInsightsOptions"/> to <see cref="ApplicationInsightsServiceOptions"/>.
    /// </summary>
    /// <param name="services">DI container.</param>
    private static void ConfigureTelemetryOptions(IServiceCollection services)
    {
        services
           .AddOptions<ApplicationInsightsServiceOptions>()
           .Configure<IOptions<ApplicationInsightsOptions>>((telemetryOptions, appOptions) =>
                                                                ApplyTelemetryOptions(telemetryOptions,
                                                                 appOptions.Value));
    }

    /// <summary>
    /// Applies configuration values to Application Insights telemetry options.
    /// </summary>
    /// <param name="telemetryOptions">The AI telemetry options to configure.</param>
    /// <param name="config">The bound application insights configuration.</param>
    private static void ApplyTelemetryOptions(ApplicationInsightsServiceOptions telemetryOptions,
                                              ApplicationInsightsOptions config)
    {
        if (!string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            telemetryOptions.ConnectionString = config.ConnectionString;
        }

        telemetryOptions.EnableAdaptiveSampling = config.EnableAdaptiveSampling;
        telemetryOptions.EnablePerformanceCounterCollectionModule = config.EnablePerformanceCounterCollectionModule;
        telemetryOptions.EnableAzureInstanceMetadataTelemetryModule = config.EnableAzureInstanceMetadataTelemetryModule;
        telemetryOptions.EnableDiagnosticsTelemetryModule = config.EnableDiagnosticsTelemetryModule;
        telemetryOptions.EnableHeartbeat = config.EnableHeartbeat;
        telemetryOptions.EnableQuickPulseMetricStream = config.EnableQuickPulseMetricStream;
    }

    /// <summary>
    /// Configures logging filters to reduce noise and control AI log levels.
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="appCategory">Logger category prefix for app logs.</param>
    private static void ConfigureLoggingFilters(IServiceCollection services, string appCategory)
    {
        services.Configure<LoggerFilterOptions>(options =>
                                                {
                                                    // Mute noisy Microsoft infrastructure logs globally.
                                                    options.Rules.Add(new LoggerFilterRule(null,
                                                                       "Microsoft",
                                                                       LogLevel.Error,
                                                                       null));

                                                    // Mute EF Core logs globally (SQL/command spam).
                                                    options.Rules.Add(new LoggerFilterRule(null,
                                                                       "Microsoft.EntityFrameworkCore",
                                                                       LogLevel.Warning,
                                                                       null));

                                                    // Ensure app logs
                                                    options.Rules.Add(new LoggerFilterRule(null,
                                                                       appCategory,
                                                                       LogLevel.Debug,
                                                                       null));

                                                    // AI provider filters (optional)
                                                    options.Rules
                                                           .Add(new
                                                                    LoggerFilterRule("Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                                                                     null,
                                                                     LogLevel.Information,
                                                                     null));
                                                });
    }

    #endregion
}
