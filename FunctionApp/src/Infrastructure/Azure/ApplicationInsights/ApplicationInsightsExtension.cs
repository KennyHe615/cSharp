using Configuration.Options;

using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Azure.ApplicationInsights;

/// <summary>
/// Extension methods for wiring up Azure Application Insights (telemetry + logging) for a .NET isolated Azure Functions app.
/// </summary>
public static class ApplicationInsightsExtension
{
    private static readonly string[] ApplicationInsightsProviders =
    [
        "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider", "ApplicationInsights"
    ];

    /// <summary>
    /// Registers Application Insights telemetry services, binds <see cref="ApplicationInsightsServiceOptions"/> from
    /// <see cref="Configuration.Options.ApplicationInsightsOptions"/>, and configures logging filter rules to reduce noise.
    /// </summary>
    /// <remarks>
    /// In .NET isolated Functions, <see cref="FunctionsApplicationInsightsExtensions.ConfigureFunctionsApplicationInsights(IServiceCollection)"/>
    /// is required to bridge worker logs to the host pipeline so they are captured by Application Insights.
    /// </remarks>
    /// <param name="services">The DI service collection to configure.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static void AddApplicationInsights(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApplicationInsightsTelemetryWorkerService();

        ConfigureTelemetryOptions(services);

        services.ConfigureFunctionsApplicationInsights();

        ConfigureLoggingFilters(services);
    }

    #region ========== *** Private Methods *** ==========

    private static void ConfigureTelemetryOptions(IServiceCollection services)
    {
        services.AddOptions<ApplicationInsightsServiceOptions>()
                .Configure<IOptions<ApplicationInsightsOptions>>((telemetryOptions, appOptions) =>
                                                                     ApplyTelemetryOptions(
                                                                         telemetryOptions,
                                                                         appOptions.Value));
    }

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

    private static void ConfigureLoggingFilters(IServiceCollection services)
    {
        services.Configure<LoggerFilterOptions>(AddLoggingRules);
    }

    private static void AddLoggingRules(LoggerFilterOptions options)
    {
        // Global defaults: keep app diagnostics verbose, mute noisy platform logs.
        // NOTE: Filter rule ordering matters; add more specific rules before broader ones.
        // For Application Insights, LogLevel.Information == 1, therefore, using Error level to mute noise

        // Mute noisy Microsoft infrastructure logs globally.
        options.Rules.Add(new LoggerFilterRule(null, "Microsoft", LogLevel.Error, null));

        // Mute EF Core logs globally (SQL/command spam).
        options.Rules.Add(new LoggerFilterRule(null, "Microsoft.EntityFrameworkCore", LogLevel.Warning, null));

        // Ensure app logs (your categories under "FunctionApp") remain visible locally.
        options.Rules.Add(new LoggerFilterRule(null, "FunctionApp", LogLevel.Debug, null));

        // Ensure Infrastructure logs remain visible locally (for Console).
        // options.Rules.Add(new LoggerFilterRule(null, "Infrastructure", LogLevel.Debug, null));

        foreach (string provider in ApplicationInsightsProviders)
        {
            // Provider-wide minimum for AI (increase to Information/Warning if telemetry volume is too high).
            options.Rules.Add(new LoggerFilterRule(provider, null, LogLevel.Debug, null));

            // For AI, ensure Microsoft / EF Core remain muted.
            options.Rules.Add(new LoggerFilterRule(provider, "Microsoft", LogLevel.Error, null));
            options.Rules.Add(new LoggerFilterRule(provider, "Microsoft.EntityFrameworkCore", LogLevel.Warning, null));
        }
    }

    #endregion
}
