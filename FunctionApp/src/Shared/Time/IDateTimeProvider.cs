namespace Shared.Time;

public interface IDateTimeProvider
{
    public TimeZoneInfo Eastern { get; }

    public DateTime UtcNow { get; }

    public DateTime EstNow { get; }

    public DateTimeOffset UtcNowOffset { get; }

    public DateTimeOffset EstNowOffset { get; }
}
