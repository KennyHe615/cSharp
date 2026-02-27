using System.ComponentModel.DataAnnotations;


namespace Infrastructure.Configuration.Options;

/// <summary>
/// Represents the configuration options for Application Insights telemetry.
/// </summary>
public sealed class ApplicationInsightsOptions
{
    /// <summary>
    /// The name of the configuration section.
    /// </summary>
    public const string SectionName = "ApplicationInsights";

    /// <summary>
    /// Gets or sets the connection string used for connecting to the Application Insights resource.
    /// </summary>
    [Required(ErrorMessage = "Application Insights connection string is required")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether adaptive sampling is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableAdaptiveSampling { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the performance counter collection module is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnablePerformanceCounterCollectionModule { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Azure Instance Metadata telemetry module is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableAzureInstanceMetadataTelemetryModule { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the diagnostics telemetry module is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableDiagnosticsTelemetryModule { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether heartbeat telemetry is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableHeartbeat { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Quick Pulse (Live Metrics) stream is enabled. Defaults to <c>true</c>.
    /// </summary>
    public bool EnableQuickPulseMetricStream { get; set; } = true;

    /// <summary>
    /// Gets or sets the sampling percentage for telemetry. Must be between 1 and 100. Defaults to <c>100</c>.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Sampling rate must be between 1-100%")]
    public int SamplingPercentage { get; set; } = 100;
}
