using System.Globalization;


namespace FunctionApp.Infrastructure.Providers;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => DateTime.Now;

    public string FormatUtcTimestamp(DateTime? dateTime = null)
    {
        var dt = dateTime ?? UtcNow;

        return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    public string FormatLocalTimestamp(DateTime? dateTime = null)
    {
        var dt = dateTime ?? Now;

        return dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
