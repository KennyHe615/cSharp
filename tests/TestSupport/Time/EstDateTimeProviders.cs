using System.Diagnostics.CodeAnalysis;

using SharedKernel.Time;


namespace tests.TestSupport.Time;

[ExcludeFromCodeCoverage]
public sealed class FixedEstDateTimeProvider : IDateTimeProvider
{
    public TimeZoneInfo Eastern =>
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime EstNow => TimeZoneInfo.ConvertTime(UtcNow, Eastern);

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public DateTimeOffset EstNowOffset => TimeZoneInfo.ConvertTime(UtcNowOffset, Eastern);

    public DateTimeOffset? ConvertToEst(DateTimeOffset? utc)
    {
        return utc.HasValue ? ConvertToEst(utc.Value) : null;
    }

    public DateTimeOffset ConvertToEst(DateTimeOffset utc)
    {
        return TimeZoneInfo.ConvertTime(utc, Eastern);
    }
}

[ExcludeFromCodeCoverage]
public sealed class SequenceEstDateTimeProvider(IEnumerable<DateTimeOffset> estNowValues) : IDateTimeProvider
{
    private readonly Queue<DateTimeOffset> _queue = new Queue<DateTimeOffset>(estNowValues);

    public TimeZoneInfo Eastern =>
        TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime EstNow => TimeZoneInfo.ConvertTime(UtcNow, Eastern);

    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;

    public DateTimeOffset EstNowOffset => _queue.Count > 0
        ? _queue.Dequeue()
        : throw new InvalidOperationException("No more test timestamps available.");

    public DateTimeOffset? ConvertToEst(DateTimeOffset? utc)
    {
        return utc.HasValue ? ConvertToEst(utc.Value) : null;
    }

    public DateTimeOffset ConvertToEst(DateTimeOffset utc)
    {
        return TimeZoneInfo.ConvertTime(utc, Eastern);
    }
}
