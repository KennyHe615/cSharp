namespace Application.Common.Abstractions.Providers;

/// <summary>
/// Defines a provider for retrieving hit counts from data sources.
/// </summary>
public interface IHitCountProvider
{
    /// <summary>
    /// Retrieves the total hit count for the specified time interval.
    /// </summary>
    /// <param name="start">The start time of the interval (inclusive).</param>
    /// <param name="end">The end time of the interval (inclusive).</param>
    /// <param name="ct">The cancellation token to abort the operation.</param>
    /// <returns>
    /// The total number of hits for the specified interval;
    /// </returns>
    Task<long> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);
}
