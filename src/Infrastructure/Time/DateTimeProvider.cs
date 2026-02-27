using System.Runtime.InteropServices;

using SharedKernel.Time;


namespace Infrastructure.Time;

public class DateTimeProvider : IDateTimeProvider
{
    public TimeZoneInfo Eastern => GetEasternTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public DateTime EstNow => TimeZoneInfo.ConvertTime(UtcNow, Eastern);

    public DateTimeOffset EstNowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, Eastern);

    private static TimeZoneInfo GetEasternTimeZone()
    {
        // Windows time zone id
        const string windowsId = "Eastern Standard Time";
        // IANA time zone id (Linux/macOS)
        const string ianaId = "America/New_York";

        string id = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windowsId : ianaId;

        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    public DateTimeOffset? ConvertToEst(DateTimeOffset? utc)
    {
        if (!utc.HasValue) return null;

        return TimeZoneInfo.ConvertTime(utc.Value, GetEasternTimeZone());
    }

    public DateTimeOffset ConvertToEst(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, GetEasternTimeZone());
}
