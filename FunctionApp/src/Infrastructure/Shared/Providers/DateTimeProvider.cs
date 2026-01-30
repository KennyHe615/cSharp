using System.Globalization;

using Application.Shared.Providers;


namespace Infrastructure.Shared.Providers;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.Now;

    public DateTimeOffset OffsetNow => DateTimeOffset.Now;

    public TimeSpan LocalOffset => DateTimeOffset.Now.Offset;

    public string FormatUtcTimestamp(DateTime? dateTime = null)
    {
        DateTime dt = dateTime ?? UtcNow;

        return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public string FormatLocalTimestamp(DateTime? dateTime = null)
    {
        DateTime dt = dateTime ?? Now;

        return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
