using System.Globalization;
using System.Text.Json.Serialization;

using SharedKernel.Serialization.Json;


namespace SharedKernel.Time;

/// <summary>
/// Represents an immutable UTC time interval with inclusive start and exclusive end semantics.
/// </summary>
/// <remarks>
/// Both <see cref="Start"/> and <see cref="End"/> must be UTC (<c>+00:00</c>) and
/// <see cref="Start"/> must be earlier than <see cref="End"/>.
/// </remarks>
[JsonConverter(typeof(UtcIntervalJsonConverter))]
public readonly record struct UtcInterval
{
    private static readonly string[] SupportedFormats =
    [
        "yyyy-MM-ddTHH:mmZ",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.SSSZ"
    ];

    /// <summary>
    /// Gets the UTC interval start time.
    /// </summary>
    public DateTimeOffset Start { get; }

    /// <summary>
    /// Gets the UTC interval end time.
    /// </summary>
    public DateTimeOffset End { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UtcInterval"/> struct.
    /// </summary>
    /// <param name="start">UTC interval start.</param>
    /// <param name="end">UTC interval end.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="start"/> or <paramref name="end"/> is not UTC,
    /// or when <paramref name="start"/> is not earlier than <paramref name="end"/>.
    /// </exception>
    public UtcInterval(DateTimeOffset start, DateTimeOffset end)
    {
        if (start.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Start must be UTC (+00:00).", nameof(start));
        }

        if (end.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("End must be UTC (+00:00).", nameof(end));
        }

        if (start >= end)
        {
            throw new ArgumentException("Start must be before End.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Parses a UTC interval string in the form <c>start/end</c>.
    /// </summary>
    /// <param name="value">Interval text.</param>
    /// <returns>Parsed <see cref="UtcInterval"/>.</returns>
    /// <exception cref="FormatException">Thrown when the value is not a valid UTC interval.</exception>
    public static UtcInterval Parse(string value)
    {
        if (TryParse(value, out UtcInterval interval)) return interval;

        throw new
                FormatException("Invalid interval format. Expected UTC interval: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");
    }

    /// <summary>
    /// Tries to parse a UTC interval string in the form <c>start/end</c>.
    /// </summary>
    /// <param name="value">Interval text.</param>
    /// <param name="interval">Parsed interval when successful; otherwise default value.</param>
    /// <returns><c>true</c> if parsing succeeds; otherwise <c>false</c>.</returns>
    public static bool TryParse(string? value, out UtcInterval interval)
    {
        interval = default;

        if (string.IsNullOrWhiteSpace(value)) return false;

        string[] parts = value.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2) return false;

        if (!TryParseUtc(parts[0], out DateTimeOffset start) || !TryParseUtc(parts[1], out DateTimeOffset end))
        {
            return false;
        }

        try
        {
            interval = new UtcInterval(start, end);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Formats the interval as <c>yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ</c>.
    /// </summary>
    /// <returns>Formatted UTC interval string.</returns>
    public override string ToString()
    {
        return $@"{Start.UtcDateTime:yyyy-MM-ddTHH:mm\Z}/{End.UtcDateTime:yyyy-MM-ddTHH:mm\Z}";
    }

    /// <summary>
    /// Normalizes a UTC interval string to the canonical <see cref="ToString"/> format.
    /// </summary>
    /// <param name="value">Interval text.</param>
    /// <returns>Canonical UTC interval text.</returns>
    /// <exception cref="FormatException">Thrown when the value is not a valid UTC interval.</exception>
    public static string Normalize(string value)
    {
        return Parse(value)
               .ToString();
    }

    #region ========== *** Private Section *** ==========

    private static bool TryParseUtc(string value, out DateTimeOffset parsed)
    {
        bool ok = DateTimeOffset.TryParseExact(value,
                                               SupportedFormats,
                                               CultureInfo.InvariantCulture,
                                               DateTimeStyles.AssumeUniversal,
                                               out parsed);

        return ok && parsed.Offset == TimeSpan.Zero;
    }

    #endregion
}
