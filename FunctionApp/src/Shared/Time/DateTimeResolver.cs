using System.Runtime.InteropServices;


namespace Shared.Time;

public static class DateTimeResolver
{
    public static DateTimeOffset? ConvertToEst(DateTimeOffset? utc)
    {
        if (!utc.HasValue) return null;

        return TimeZoneInfo.ConvertTime(utc.Value, GetEasternTimeZone());
    }

    public static DateTimeOffset ConvertToEst(DateTimeOffset utc)
    {
        return TimeZoneInfo.ConvertTime(utc, GetEasternTimeZone());
    }

    public static DateTimeOffset ConvertToEstAndRoundToSecond(DateTimeOffset utc)
    {
        DateTimeOffset est = ConvertToEst(utc);

        return RoundToSeconds(est);
    }

    public static TimeZoneInfo GetEasternTimeZone()
    {
        // Windows time zone id
        const string windowsId = "Eastern Standard Time";
        // IANA time zone id (Linux/macOS)
        const string ianaId = "America/New_York";

        string id = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windowsId : ianaId;

        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    public static DateTimeOffset NormalizeToMinute(DateTimeOffset dateTime)
    {
        return new DateTimeOffset(dateTime.Year,
                                  dateTime.Month,
                                  dateTime.Day,
                                  dateTime.Hour,
                                  dateTime.Minute,
                                  0,
                                  0,
                                  dateTime.Offset);
    }

    #region ========== *** Private Methods *** ==========

    private static DateTimeOffset RoundToSeconds(DateTimeOffset dateTime)
    {
        long ticks = dateTime.Ticks;
        const long ticksPerSecond = TimeSpan.TicksPerSecond;
        long remainder = ticks % ticksPerSecond;

        long roundedTicks = remainder >= ticksPerSecond / 2
            ? ticks + (ticksPerSecond - remainder) // Round UP if >= 500ms
            : ticks - remainder; // Round DOWN if < 500ms

        return new DateTimeOffset(roundedTicks, dateTime.Offset);
    }

    #endregion
}
