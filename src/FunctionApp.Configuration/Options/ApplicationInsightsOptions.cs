namespace FunctionApp.Configuration.Options
{
    /// <summary>
    /// Configuration options for Application Insights
    /// </summary>
    public class ApplicationInsightsOptions
    {
        public const string SectionName = "ApplicationInsights";

        /// <summary>
        /// Connection string for Application Insights
        /// </summary>
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
    }
}
