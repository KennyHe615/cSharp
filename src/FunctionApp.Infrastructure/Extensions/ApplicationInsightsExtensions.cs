using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Infrastructure.Extensions;

public static class ApplicationInsightsExtensions
{
    private static readonly JsonSerializerOptions _jsonOption = new JsonSerializerOptions
                                                                {
                                                                    WriteIndented = true,
                                                                    IndentSize = 4,
                                                                };

    public static void AddApplicationInsightsCustom(this IServiceCollection services)
    {
        services.AddApplicationInsightsTelemetryWorkerService(options =>
                                                              {
                                                                  // Configure telemetry options
                                                                  options.EnableAdaptiveSampling =
                                                                      false;// Disable sampling to ensure all logs are captured
                                                                  options.EnablePerformanceCounterCollectionModule = false;
                                                                  options.EnableAzureInstanceMetadataTelemetryModule = false;
                                                                  options.EnableDiagnosticsTelemetryModule = false;
                                                                  options.EnableHeartbeat = false;
                                                                  options.EnableQuickPulseMetricStream = false;
                                                              });

        // Configure logging specifically for Application Insights
        services.Configure<LoggerFilterOptions>(options =>
                                                {
                                                    // Ensure Information level logs reach Application Insights
                                                    options.Rules.Add(new
                                                                          LoggerFilterRule("Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                                                                           "FunctionApp",
                                                                           LogLevel.Information,
                                                                           null));

                                                    options.Rules
                                                           .Add(new
                                                                    LoggerFilterRule("Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                                                                                     null,
                                                                                     LogLevel.Information,
                                                                                     null));
                                                });
    }

    // Convenience methods for common log levels
    public static void LogInfoStructuredDetails(this ILogger logger, object data)
    {
        logger.LogStructuredMessage(LogLevel.Information, data);
    }

    public static void LogWarningStructuredDetails(this ILogger logger, object data)
    {
        logger.LogStructuredMessage(LogLevel.Warning, data);
    }

    public static void LogErrorStructuredDetails(this ILogger logger, object data)
    {
        logger.LogStructuredMessage(LogLevel.Error, data);
    }

    public static void LogCriticalStructuredDetails(this ILogger logger, object data)
    {
        logger.LogStructuredMessage(LogLevel.Critical, data);
    }

    #region ========== *** Private Methods *** ==========

    private static void LogStructuredMessage(this ILogger logger, LogLevel logLevel, object data)
    {
        var msg = JsonSerializer.Serialize(data, _jsonOption);

        switch (logLevel)
        {
            case LogLevel.Trace:
                logger.LogTrace("========== Execution Details ==========");
                logger.LogTrace("{Msg}", msg);

                break;

            case LogLevel.Debug:
                logger.LogDebug("========== Execution Details ==========");
                logger.LogDebug("{Msg}", msg);

                break;

            case LogLevel.Information:
                logger.LogInformation("========== Execution Details ==========");
                logger.LogInformation("{Msg}", msg);

                break;

            case LogLevel.Warning:
                logger.LogWarning("========== Execution Details ==========");
                logger.LogWarning("{Msg}", msg);

                break;

            case LogLevel.Error:
                logger.LogError("========== Execution Details ==========");
                logger.LogError("{Msg}", msg);

                break;

            case LogLevel.Critical:
                logger.LogCritical("========== Execution Details ==========");
                logger.LogCritical("{Msg}", msg);

                break;

            case LogLevel.None:
            default:
                logger.LogInformation("========== Execution Details ==========");
                logger.LogInformation("{Msg}", msg);

                break;
        }
    }

    #endregion
}
