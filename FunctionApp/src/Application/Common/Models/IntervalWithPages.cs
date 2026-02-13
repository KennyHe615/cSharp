using Shared.Constants;


namespace Application.Common.Models;

/// <summary>
/// Represents an interval with calculated pagination information based on hit count.
/// </summary>
public sealed record IntervalWithPages(Interval Interval,
                                       int TotalPages)
{
    /// <summary>
    /// Creates an <see cref="IntervalWithPages"/> instance from an interval and total hit count.
    /// </summary>
    /// <param name="interval">The time interval.</param>
    /// <param name="totalHits">The total number of hits for the interval.</param>
    /// <returns>An <see cref="IntervalWithPages"/> instance with calculated page count.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="interval"/> is null.</exception>
    public static IntervalWithPages Create(Interval interval, long totalHits)
    {
        ArgumentNullException.ThrowIfNull(interval);

        const int pageSize = GenesysConstants.DefaultPageSize;

        int totalPages = totalHits > 0 ? (int)Math.Ceiling((double)totalHits / pageSize) : 0;

        return new IntervalWithPages(interval, totalPages);
    }
}
