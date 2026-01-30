namespace Application.Shared.Providers;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateTime Now { get; }

    DateTimeOffset OffsetNow { get; }

    TimeSpan LocalOffset { get; }

    string FormatUtcTimestamp(DateTime? dateTime = null);

    string FormatLocalTimestamp(DateTime? dateTime = null);
}
