namespace Shared.Time;

public class DateTimeProvider : IDateTimeProvider
{
    public TimeZoneInfo Eastern => DateTimeResolver.GetEasternTimeZone();

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public DateTime EstNow => TimeZoneInfo.ConvertTime(UtcNow, Eastern);

    public DateTimeOffset EstNowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, Eastern);
}
