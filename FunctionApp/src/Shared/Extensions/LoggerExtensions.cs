using System.Diagnostics;

using Microsoft.Extensions.Logging;


namespace Shared.Extensions;

/// <summary>
/// Provides extension methods for enhanced logging functionality with structured context and exception handling.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Creates a logging scope with operation context and distributed tracing information.
    /// Automatically starts a new Activity if one is not already present.
    /// </summary>
    /// <param name="logger">The logger instance to create the scope for.</param>
    /// <param name="category">The name of the category being performed.</param>
    /// <param name="lobName">The line of business name, if applicable.</param>
    /// <returns>An IDisposable that ends the scope and stops any created Activity when disposed.</returns>
    public static IDisposable BeginOperationScope(this ILogger logger, string category, string? lobName)
    {
        if (string.IsNullOrWhiteSpace(category)) category = "UnknownOperation";

        Activity? activity = Activity.Current;
        bool startedHere = false;

        if (activity == null)
        {
            activity = new Activity(category);
            activity.Start();
            startedHere = true;
        }

        IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
                                               {
                                                   ["Lob_Name"] = lobName,
                                                   ["Category_Name"] = category,
                                                   ["TraceId"] = activity.TraceId.ToString(),
                                                   ["SpanId"] = activity.SpanId.ToString(),
                                                   ["ActivityId"] = activity.Id
                                               });

        if (!startedHere) return scope ?? NoopDisposable.Instance;

        ActivityStopper stop = new(activity);

        return scope is null ? stop : new CompositeDisposable(scope, stop);
    }

    /// <summary>
    /// Logs a warning message with detailed exception information and structured context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">Optional additional arguments to include in the log.</param>
    public static void LogWarningWithDetails(this ILogger logger,
                                             Exception exception,
                                             string message,
                                             params object?[] args)
    {
        LogWithDetails(logger, LogLevel.Warning, exception, message, args);
    }

    /// <summary>
    /// Logs an error message with detailed exception information and structured context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">Optional additional arguments to include in the log.</param>
    public static void LogErrorWithDetails(this ILogger logger,
                                           Exception exception,
                                           string message,
                                           params object?[] args)
    {
        LogWithDetails(logger, LogLevel.Error, exception, message, args);
    }

    /// <summary>
    /// Logs a critical message with detailed exception information and structured context.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">Optional additional arguments to include in the log.</param>
    public static void LogCriticalWithDetails(this ILogger logger,
                                              Exception exception,
                                              string message,
                                              params object?[] args)
    {
        LogWithDetails(logger, LogLevel.Critical, exception, message, args);
    }

    #region ========== *** Private *** ==========

    /// <summary>
    /// Internal method that logs exception details with structured scope including summary, root cause, and full details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="level">The log level to use.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="args">Optional additional arguments to include in the log.</param>
    private static void LogWithDetails(ILogger logger,
                                       LogLevel level,
                                       Exception exception,
                                       string message,
                                       object?[] args)
    {
        using (logger.BeginScope(exception.ToLogScope()))
        {
#pragma warning disable CA2254
            logger.Log(level, exception, message, args);
#pragma warning restore CA2254

            if (args is { Length: > 0 })
            {
                logger.Log(level, exception, "{CallerArgs}", args);
            }

            logger.Log(level, "Exception Summary: {Summary}", exception.ToSummary());

            Exception rootCause = exception.GetRootCause();
            if (!ReferenceEquals(rootCause, exception))
            {
                logger.Log(level,
                           "Root Cause: {RootType} - {RootMessage}",
                           rootCause.GetType().Name,
                           rootCause.Message);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Full Exception Details:\n{Details}", exception.ToReadableString());
            }
        }
    }

    /// <summary>
    /// Disposable wrapper that stops an Activity when disposed.
    /// </summary>
    /// <param name="activity">The Activity to stop on disposal.</param>
    private sealed class ActivityStopper(Activity activity) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            activity.Stop();
        }
    }

    /// <summary>
    /// Disposable wrapper that disposes multiple IDisposable instances in sequence.
    /// </summary>
    /// <param name="first">The first disposable to dispose.</param>
    /// <param name="second">The second disposable to dispose.</param>
    private sealed class CompositeDisposable(IDisposable first,
                                             IDisposable second) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                first.Dispose();
            }
            finally
            {
                second.Dispose();
            }
        }
    }

    /// <summary>
    /// No-operation disposable implementation that does nothing when disposed.
    /// </summary>
    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }

    #endregion
}
