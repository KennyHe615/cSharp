using Application.Common.Factories;


namespace Application.Common.Models;

/// <summary>
/// Represents an ISO 8601 time interval in UTC format.
/// </summary>
/// <remarks>
/// <para>
/// This record encapsulates a time interval with a start and end time, both in UTC.
/// Valid intervals must satisfy the following constraints:
/// </para>
/// <list type="bullet">
/// <item><description>Start time must be before End time</description></item>
/// <item><description>Both times must be in UTC (offset +00:00)</description></item>
/// </list>
/// <para>
/// Use <see cref="IntervalFactory"/> to create and validate instances.
/// </para>
/// </remarks>
/// <param name="Start">The interval start time in UTC (inclusive).</param>
/// <param name="End">The interval end time in UTC (inclusive).</param>
public record Interval(DateTimeOffset Start,
                       DateTimeOffset End)
{
    /// <summary>
    /// Converts the interval to ISO 8601 interval string format without seconds.
    /// </summary>
    /// <returns>
    /// A string in the format "yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ" representing the interval.
    /// </returns>
    /// <remarks>
    /// The output format uses UTC timezone indicator 'Z' and excludes seconds for brevity.
    /// Both start and end times are formatted identically.
    /// </remarks>
    /// <example>2025-08-18T04:00Z/2025-08-19T04:00Z</example>
    public override string ToString()
    {
        return $@"{Start.UtcDateTime:yyyy-MM-ddTHH:mm\Z}/{End.UtcDateTime:yyyy-MM-ddTHH:mm\Z}";
    }

    /// <summary>
    /// Gets the duration of the interval as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <value>
    /// The time span between <see cref="End"/> and <see cref="Start"/>.
    /// </value>
    /// <remarks>
    /// This is a computed property that calculates the difference between the end and start times.
    /// For valid intervals, this value will always be positive.
    /// </remarks>
    public TimeSpan Duration => End - Start;
}
