namespace FunctionApp.Infrastructure.Providers;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }

    DateTime Now { get; }

    string FormatUtcTimestamp(DateTime? dateTime = null);

    string FormatLocalTimestamp(DateTime? dateTime = null);
}
