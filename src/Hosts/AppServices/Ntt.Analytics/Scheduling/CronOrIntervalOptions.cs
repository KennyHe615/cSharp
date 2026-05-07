using System.ComponentModel.DataAnnotations;


namespace Ntt.Analytics.Scheduling;

/// <summary>
/// Configurable scheduling intervals for NTT analytics background workers.
/// </summary>
public sealed class CronOrIntervalOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Scheduling";

    /// <summary>
    /// Gets the execution interval, in minutes, for UsersDetails incremental scans.
    /// </summary>
    [Range(1, 1440)]
    public int UsersDetailsIncrementalIntervalMinutes { get; init; } = 30;

    /// <summary>
    /// Gets the execution interval, in hours, for UsersDetails proactive recovery scans.
    /// This can be set to <c>3</c> or <c>6</c> based on the finalized operating schedule.
    /// </summary>
    [Range(1, 168)]
    public int UsersDetailsRecoveryIntervalHours { get; init; } = 3;
}
