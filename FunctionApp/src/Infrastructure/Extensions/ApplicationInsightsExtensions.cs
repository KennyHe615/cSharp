using Configuration.Options;

using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring Application Insights telemetry and logging filters.
/// </summary>
public static class ApplicationInsightsExtensions
{
    /// <summary>
    /// Registers Application Insights telemetry services, configures telemetry options based on application settings,
    /// and sets up specific logging filter rules for Application Insights providers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    public static void AddApplicationInsights(this IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        services.AddOptions<ApplicationInsightsServiceOptions>()
                .Configure<IOptions<ApplicationInsightsOptions>>((telemetryOptions, appOptions) =>
                                                                 {
                                                                     ApplicationInsightsOptions config =
                                                                         appOptions.Value;

                                                                     if (!string.IsNullOrEmpty(config.ConnectionString))
                                                                     {
                                                                         telemetryOptions.ConnectionString =
                                                                             config.ConnectionString;
                                                                     }

                                                                     telemetryOptions.EnableAdaptiveSampling =
                                                                         config.EnableAdaptiveSampling;
                                                                     telemetryOptions
                                                                             .EnablePerformanceCounterCollectionModule =
                                                                         config
                                                                             .EnablePerformanceCounterCollectionModule;
                                                                     telemetryOptions
                                                                             .EnableAzureInstanceMetadataTelemetryModule =
                                                                         config
                                                                             .EnableAzureInstanceMetadataTelemetryModule;
                                                                     telemetryOptions.EnableDiagnosticsTelemetryModule =
                                                                         config.EnableDiagnosticsTelemetryModule;
                                                                     telemetryOptions.EnableHeartbeat =
                                                                         config.EnableHeartbeat;
                                                                     telemetryOptions.EnableQuickPulseMetricStream =
                                                                         config.EnableQuickPulseMetricStream;
                                                                 });

        // This call is essential for .NET Isolated workers to bridge worker logs to the host telemetry
        services.ConfigureFunctionsApplicationInsights();

        // Configure logging specifically for Application Insights
        services.Configure<LoggerFilterOptions>(options =>
                                                {
                                                    // List of providers associated with Application Insights
                                                    string[] aiProviders =
                                                    [
                                                        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                                                        "ApplicationInsights"
                                                    ];

                                                    // For Application Insights, LogLevel.Information == 1
                                                    // Therefore, using Error level to mute noise

                                                    // Global rules (affect all providers)
                                                    // Ensure local/application logs reach Debug level
                                                    options.Rules.Add(
                                                        new LoggerFilterRule(
                                                            null,
                                                            "FunctionApp",
                                                            LogLevel.Debug,
                                                            null));

                                                    // Mute noisy Microsoft infrastructure logs globally
                                                    options.Rules.Add(
                                                        new LoggerFilterRule(null, "Microsoft", LogLevel.Error, null));

                                                    // Mute EF Core logs globally to prevent overwhelming the telemetry
                                                    options.Rules.Add(
                                                        new LoggerFilterRule(
                                                            null,
                                                            "Microsoft.EntityFrameworkCore",
                                                            LogLevel.Error,
                                                            null));

                                                    foreach (string provider in aiProviders)
                                                    {
                                                        // Ensure Debug level logs reach AI by default for better observability
                                                        options.Rules.Add(
                                                            new LoggerFilterRule(provider, null, LogLevel.Debug, null));

                                                        // Filter out Microsoft infrastructure logs (like "Content root path") specifically for AI providers
                                                        options.Rules.Add(
                                                            new LoggerFilterRule(
                                                                provider,
                                                                "Microsoft",
                                                                LogLevel.Error,
                                                                null));

                                                        // Override EF Core logs to Error level for AI providers
                                                        // This prevents broad rules from pulling in verbose SQL execution details
                                                        options.Rules.Add(
                                                            new LoggerFilterRule(
                                                                provider,
                                                                "Microsoft.EntityFrameworkCore",
                                                                LogLevel.Error,
                                                                null));
                                                    }
                                                });
    }
}
