using System.ComponentModel.DataAnnotations;


namespace FunctionApp.Configuration.Options
{
    /// <summary>
    /// Configuration options for Application Insights
    /// </summary>
    public sealed class ApplicationInsightsOptions
    {
        public const string SectionName = "ApplicationInsights";

        /// <summary>
        /// Connection string for Application Insights
        /// </summary>
        [Required(ErrorMessage = "Application Insights connection string is required")]
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Enable adaptive sampling
        /// </summary>
        public bool EnableAdaptiveSampling { get; set; } = true;

        /// <summary>
        /// Enable a performance counter collection
        /// </summary>
        public bool EnablePerformanceCounterCollectionModule { get; set; } = true;

        /// <summary>
        /// Enable Azure instance metadata telemetry
        /// </summary>
        public bool EnableAzureInstanceMetadataTelemetryModule { get; set; } = true;

        /// <summary>
        /// Enable diagnostics telemetry module
        /// </summary>
        public bool EnableDiagnosticsTelemetryModule { get; set; } = true;

        /// <summary>
        /// Enable heartbeat
        /// </summary>
        public bool EnableHeartbeat { get; set; } = true;

        /// <summary>
        /// Enable quick pulse metric stream
        /// </summary>
        public bool EnableQuickPulseMetricStream { get; set; } = true;

        /// <summary>
        /// Sampling rate percentage (1-100)
        /// </summary>
        [Range(1, 100, ErrorMessage = "Sampling rate must be between 1-100%")]
        public int SamplingPercentage { get; set; } = 100;
    }
}
