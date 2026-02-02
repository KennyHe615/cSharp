using System.Runtime.InteropServices;


namespace Shared.Providers;

public class DateTimeProvider : IDateTimeProvider
{
    private static readonly Lazy<TimeZoneInfo> EasternLazy = new(ResolveEasternTimeZone);

    public TimeZoneInfo Eastern => EasternLazy.Value;

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public DateTime EstNow => TimeZoneInfo.ConvertTime(UtcNow, Eastern);

    public DateTimeOffset EstNowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, Eastern);

    public DateTimeOffset? ConvertToEst(DateTimeOffset? utc)
    {
        if (!utc.HasValue) return null;

        return TimeZoneInfo.ConvertTime(utc.Value, Eastern);
    }

    #region ========== *** Private Methods *** ==========

    private static TimeZoneInfo ResolveEasternTimeZone()
    {
        // Windows time zone id
        const string windowsId = "Eastern Standard Time";
        // IANA time zone id (Linux/macOS)
        const string ianaId = "America/New_York";

        string id = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windowsId : ianaId;

        return TimeZoneInfo.FindSystemTimeZoneById(id);
    }

    #endregion
}
