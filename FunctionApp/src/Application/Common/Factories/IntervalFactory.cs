using Application.Common.Models;


namespace Application.Common.Factories;

/// <summary>
/// Factory for creating and validating ISO 8601 interval strings with strict UTC enforcement.
/// </summary>
/// <remarks>
/// All methods enforce the following business rules:
/// <list type="bullet">
/// <item><description>Start time must be before end time</description></item>
/// <item><description>Both times must be in UTC (offset +00:00)</description></item>
/// </list>
/// <para>
/// Supported input formats:
/// </para>
/// <list type="bullet">
/// <item><description>yyyy-MM-ddTHH:mmZ</description></item>
/// <item><description>yyyy-MM-ddTHH:mm:ssZ</description></item>
/// <item><description>yyyy-MM-ddTHH:mm:ss.SSSZ</description></item>
/// </list>
/// <para>
/// Output format: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ (without seconds for brevity).
/// </para>
/// </remarks>
public static class IntervalFactory
{
    private static readonly string[] SupportedFormats =
    [
        "yyyy-MM-ddTHH:mmZ", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.SSSZ"
    ];

    /// <summary>
    /// Creates a validated interval from two <see cref="DateTimeOffset"/> values.
    /// </summary>
    /// <param name="start">The interval start time in UTC.</param>
    /// <param name="end">The interval end time in UTC.</param>
    /// <returns>A validated <see cref="Interval"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when start or end time is not in UTC, or when start time is not before end time.
    /// </exception>
    public static Interval FromDateTimeOffset(DateTimeOffset start, DateTimeOffset end)
    {
        ValidateUtc(start, nameof(start));
        ValidateUtc(end, nameof(end));
        ValidateOrdering(start, end);

        return new Interval(start, end);
    }

    /// <summary>
    /// Creates a validated interval string from a single ISO 8601 interval string.
    /// </summary>
    /// <param name="intervalString">
    /// An ISO 8601 interval string in the format "start/end".
    /// Example: "2025-08-18T04:00Z/2025-08-19T04:00Z"
    /// </param>
    /// <returns>A validated <see cref="Interval"/> instance.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the interval string is invalid, or when validation rules are violated.
    /// </exception>
    public static Interval FromString(string intervalString)
    {
        if (string.IsNullOrWhiteSpace(intervalString))
        {
            throw new ArgumentException("Interval string cannot be null or empty.", nameof(intervalString));
        }

        string[] parts = intervalString.Split('/',
                                              StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid interval format. Expected 'start/end', got: '{intervalString}'",
                                        nameof(intervalString));
        }

        DateTimeOffset start = ParseUtcDateTime(parts[0], nameof(intervalString));
        DateTimeOffset end = ParseUtcDateTime(parts[1], nameof(intervalString));

        ValidateOrdering(start, end);

        return new Interval(start, end);
    }

    /// <summary>
    /// Validates an existing <see cref="Interval"/> against all business rules.
    /// </summary>
    /// <param name="interval">The interval to validate.</param>
    /// <returns><c>true</c> if the interval is valid; otherwise, <c>false</c>.</returns>
    public static bool IsValid(Interval interval)
    {
        return interval.Start.Offset == TimeSpan.Zero &&
               interval.End.Offset == TimeSpan.Zero &&
               interval.Start < interval.End;
    }

    #region ========== *** Private Validation Methods *** ==========

    /// <summary>
    /// Validates that a <see cref="DateTimeOffset"/> is in UTC (offset +00:00).
    /// </summary>
    private static void ValidateUtc(DateTimeOffset dateTime, string paramName)
    {
        if (dateTime.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"DateTime must be in UTC (offset must be +00:00). Actual offset: {dateTime.Offset}",
                paramName);
        }
    }

    /// <summary>
    /// Validates that the start time is before the end time.
    /// </summary>
    private static void ValidateOrdering(DateTimeOffset start, DateTimeOffset end)
    {
        if (start >= end)
        {
            throw new ArgumentException("Start time must be before end time.", nameof(start));
        }
    }

    /// <summary>
    /// Attempts to parse a UTC ISO 8601 formatted string into a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <remarks>
    /// Accepts three formats: yyyy-MM-ddTHH:mmZ, yyyy-MM-ddTHH:mm:ssZ, yyyy-MM-ddTHH:mm:ss.SSSZ
    /// </remarks>
    private static DateTimeOffset ParseUtcDateTime(string value, string paramName)
    {
        if (!DateTimeOffset.TryParseExact(value,
                                          SupportedFormats,
                                          System.Globalization.CultureInfo.InvariantCulture,
                                          System.Globalization.DateTimeStyles.AssumeUniversal,
                                          out DateTimeOffset result))
        {
            throw new ArgumentException(
                $"Invalid time format: '{value}'. Expected UTC formats: {string.Join(", ", SupportedFormats)}",
                paramName);
        }

        return result;
    }

    #endregion
}
