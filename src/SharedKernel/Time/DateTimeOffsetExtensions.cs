namespace SharedKernel.Time;

/// <summary>
/// Shared <see cref="DateTimeOffset"/> extensions used across scheduling,
/// interval construction, and audit-related date-time normalization.
/// </summary>
public static class DateTimeOffsetExtensions
{
    /// <summary>
    /// Calculates the duration, in whole seconds, from the supplied start timestamp
    /// to the optional end timestamp.
    /// </summary>
    /// <param name="startTime">Start timestamp.</param>
    /// <param name="endTime">Optional end timestamp.</param>
    /// <returns>
    /// Total elapsed seconds when <paramref name="endTime"/> has a value; otherwise <c>null</c>.
    /// </returns>
    public static long? CalculateDurationTo(this DateTimeOffset startTime, DateTimeOffset? endTime)
    {
        return endTime.HasValue ? (long?)(endTime.Value - startTime).TotalSeconds : null;
    }

    /// <summary>
    /// Rounds the supplied timestamp to the nearest minute while preserving its existing offset.
    /// </summary>
    /// <param name="dateTime">Timestamp to round.</param>
    /// <returns>The rounded timestamp.</returns>
    public static DateTimeOffset RoundToMinute(this DateTimeOffset dateTime)
    {
        return RoundByUnit(dateTime, TimeSpan.TicksPerMinute);
    }

    /// <summary>
    /// Truncates the supplied timestamp to minute precision while preserving its existing offset.
    /// </summary>
    /// <param name="dateTime">Timestamp to truncate.</param>
    /// <returns>The truncated timestamp with seconds removed.</returns>
    public static DateTimeOffset TruncateToMinute(this DateTimeOffset dateTime)
    {
        return new DateTimeOffset(dateTime.Year,
                                  dateTime.Month,
                                  dateTime.Day,
                                  dateTime.Hour,
                                  dateTime.Minute,
                                  0,
                                  dateTime.Offset);
    }

    /// <summary>
    /// Returns the local start-of-day boundary for the supplied timestamp while preserving its existing offset.
    /// </summary>
    /// <param name="dateTime">Timestamp whose local calendar day should be normalized.</param>
    /// <returns>The normalized start-of-day timestamp.</returns>
    public static DateTimeOffset StartOfDay(this DateTimeOffset dateTime)
    {
        return new DateTimeOffset(dateTime.Year,
                                  dateTime.Month,
                                  dateTime.Day,
                                  0,
                                  0,
                                  0,
                                  dateTime.Offset);
    }

    /// <summary>
    /// Rounds the supplied timestamp to the nearest second while preserving its existing offset.
    /// </summary>
    /// <param name="dateTime">Timestamp to round.</param>
    /// <returns>The rounded timestamp.</returns>
    public static DateTimeOffset RoundToSecond(this DateTimeOffset dateTime)
    {
        return RoundByUnit(dateTime, TimeSpan.TicksPerSecond);
    }

    /// <summary>
    /// Normalizes the supplied timestamp to UTC while preserving the instant in time.
    /// </summary>
    /// <param name="dateTime">Timestamp to normalize.</param>
    /// <returns>The timestamp represented with a UTC offset.</returns>
    public static DateTimeOffset NormalizeToUtc(this DateTimeOffset dateTime)
    {
        return dateTime.ToUniversalTime();
    }

    /// <summary>
    /// Normalizes the supplied optional timestamp to UTC while preserving the instant in time.
    /// </summary>
    /// <param name="dateTime">Optional timestamp to normalize.</param>
    /// <returns>The timestamp represented with a UTC offset when supplied; otherwise <c>null</c>.</returns>
    public static DateTimeOffset? NormalizeToUtc(this DateTimeOffset? dateTime)
    {
        return dateTime?.ToUniversalTime();
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Rounds the supplied timestamp to the nearest unit defined by <paramref name="ticksPerUnit"/>.
    /// </summary>
    /// <param name="dateTime">Timestamp to round.</param>
    /// <param name="ticksPerUnit">Tick size of the target unit.</param>
    /// <returns>The rounded timestamp.</returns>
    private static DateTimeOffset RoundByUnit(DateTimeOffset dateTime, long ticksPerUnit)
    {
        long ticks = dateTime.Ticks;
        long remainder = ticks % ticksPerUnit;

        long roundedTicks = remainder >= ticksPerUnit / 2 ? ticks + (ticksPerUnit - remainder) : ticks - remainder;

        return new DateTimeOffset(roundedTicks, dateTime.Offset);
    }

    #endregion
}
