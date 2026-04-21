namespace Application.Abstractions.Recovery;

/// <summary>
/// Defines recovery interval bounds used to validate interval-based recovery requests.
/// </summary>
/// <remarks>
/// Implementations may derive these limits from provider-specific configuration, but application
/// validation depends only on this abstraction.
/// </remarks>
public interface IRecoveryIntervalPolicy
{
    /// <summary>
    /// Gets the maximum number of days into the past that recovery requests may start.
    /// </summary>
    int HistoricalDataLimitDays { get; }

    /// <summary>
    /// Gets the number of days into the future tolerated for recovery interval end values.
    /// </summary>
    int FutureSkewDays { get; }

    /// <summary>
    /// Determines whether the supplied interval start is within the configured historical retention window.
    /// </summary>
    /// <param name="start">UTC interval start to validate.</param>
    /// <returns><c>true</c> when <paramref name="start"/> is not older than the allowed historical limit.</returns>
    bool IsStartWithinRetention(DateTimeOffset start);

    /// <summary>
    /// Determines whether the supplied interval end is within the configured future-skew allowance.
    /// </summary>
    /// <param name="end">UTC interval end to validate.</param>
    /// <returns><c>true</c> when <paramref name="end"/> does not exceed the allowed future-skew limit.</returns>
    bool IsEndWithinFutureSkew(DateTimeOffset end);
}
