using FunctionApp.Configuration.Options;

using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.Extensions;

public static class ApplicationInsightsExtensions
{
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
                                                    string[] aiProviders =
                                                    [
                                                        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                                                        "ApplicationInsights"
                                                    ];

                                                    // For Application Insights, LogLevel.Information == 1
                                                    // Therefore, using Error level to mute noise

                                                    foreach (string provider in aiProviders)
                                                    {
                                                        // Ensure Information level logs reach AI by default
                                                        options.Rules.Add(
                                                            new LoggerFilterRule(
                                                                provider,
                                                                null,
                                                                LogLevel.Information,
                                                                null));

                                                        // Filter out Microsoft infrastructure logs (like "Content root path") for AI providers
                                                        options.Rules.Add(
                                                            new LoggerFilterRule(
                                                                provider,
                                                                "Microsoft",
                                                                LogLevel.Error,
                                                                null));

                                                        // Override EF Core logs to Warning level for AI providers
                                                        // This prevents the broad rule above from pulling in EF Information logs
                                                        options.Rules.Add(
                                                            new LoggerFilterRule(
                                                                provider,
                                                                "Microsoft.EntityFrameworkCore",
                                                                LogLevel.Error,
                                                                null));
                                                    }

                                                    // Apply global filter for EF Core (affects Console and other providers)
                                                    options.Rules.Add(
                                                        new LoggerFilterRule(null, "Microsoft", LogLevel.Error, null));
                                                    options.Rules.Add(
                                                        new LoggerFilterRule(
                                                            null,
                                                            "Microsoft.EntityFrameworkCore",
                                                            LogLevel.Error,
                                                            null));
                                                });
    }
}
