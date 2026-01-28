using System.Diagnostics;

using Microsoft.Extensions.Logging;


namespace Application.Shared.Extensions;

public static class LoggerExtensions
{
    public static IDisposable BeginOperationScope(this ILogger logger, string operationName, string? lobName)
    {
        Activity? activity = Activity.Current;
        bool startedHere = false;

        if (activity == null)
        {
            activity = new Activity(operationName);
            activity.Start();
            startedHere = true;
        }

        IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
                                               {
                                                   ["Operation"] = operationName,
                                                   ["Lob"] = lobName,
                                                   ["TraceId"] = activity.TraceId.ToString(),
                                                   ["SpanId"] = activity.SpanId.ToString(),
                                                   ["ActivityId"] = activity.Id
                                               });

        if (!startedHere)
        {
            return scope ?? NoopDisposable.Instance;
        }

        ActivityStopper stop = new(activity);

        return scope is null ? stop : new CompositeDisposable(scope, stop);
    }

    #region ========== *** Private Sealed Classes *** ==========

    private sealed class ActivityStopper(Activity activity) : IDisposable
    {
        public void Dispose()
        {
            activity.Stop();
        }
    }

    private sealed class CompositeDisposable(IDisposable first,
                                             IDisposable second) : IDisposable
    {
        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
        }
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    #endregion
}
