using System.Diagnostics;

using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;


namespace SharedKernel.Logging;

public static class LoggerExtensions
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("SharedKernel.Logging");

    public static IDisposable BeginOperationScope(this ILogger logger,
                                                  LobName lob,
                                                  string? category = null,
                                                  string? entity = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        category = string.IsNullOrWhiteSpace(category) ? null : category;
        entity = string.IsNullOrWhiteSpace(entity) ? null : entity;

        Activity? existing = Activity.Current;
        Activity? started = existing is null ? ActivitySource.StartActivity(category ?? "Operation") : null;

        Activity? activity = started ?? existing;

        if (activity is not null)
        {
            activity.SetTag(LobScopeKeys.Lob, lob.Value);

            if (category is not null) activity.SetTag(LobScopeKeys.Category, category);

            if (entity is not null) activity.SetTag(LobScopeKeys.Entity, entity);
        }

        Dictionary<string, object?> scopeState = new Dictionary<string, object?>(StringComparer.Ordinal)
                                                 {
                                                     [LobScopeKeys.Lob] = lob.Value,
                                                     [LobScopeKeys.Category] = category,
                                                     [LobScopeKeys.Entity] = entity,
                                                     [LobScopeKeys.TraceId] = activity?.TraceId.ToString(),
                                                     [LobScopeKeys.SpanId] = activity?.SpanId.ToString(),
                                                     [LobScopeKeys.ActivityId] = activity?.Id
                                                 };

        IDisposable scope = logger.BeginScope(scopeState) ?? NoopDisposable.Instance;

        return started is null ? scope : new CompositeDisposable(scope, started);
    }

    // Keep your detailed exception behavior, but no CA2254 suppression.
    public static void LogErrorWithDetails(this ILogger logger,
                                           Exception ex,
                                           string messageTemplate,
                                           params object?[] args)
    {
        LogWithDetails(logger,
                       LogLevel.Error,
                       ex,
                       messageTemplate,
                       args);
    }

    public static void LogWarningWithDetails(this ILogger logger,
                                             Exception ex,
                                             string messageTemplate,
                                             params object?[] args)
    {
        LogWithDetails(logger,
                       LogLevel.Warning,
                       ex,
                       messageTemplate,
                       args);
    }

    public static void LogCriticalWithDetails(this ILogger logger,
                                              Exception ex,
                                              string messageTemplate,
                                              params object?[] args)
    {
        LogWithDetails(logger,
                       LogLevel.Critical,
                       ex,
                       messageTemplate,
                       args);
    }

    #region ========== *** Private *** ==========

    private static void LogWithDetails(ILogger logger,
                                       LogLevel level,
                                       Exception ex,
                                       string messageTemplate,
                                       object?[] args)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageTemplate);

        using (logger.BeginScope(ex.ToLogScope()))
        {
#pragma warning disable CA2254
            logger.Log(level,
                       ex,
                       messageTemplate,
                       args);
#pragma warning restore CA2254
            logger.Log(level, "Exception Summary: {Summary}", ex.ToSummary());

            Exception root = ex.GetRootCause();
            if (!ReferenceEquals(root, ex))
            {
                logger.Log(level,
                           "Root Cause: {RootType} - {RootMessage}",
                           root.GetType().Name,
                           root.Message);
            }

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Full Exception Details:\n{Details}", ex.ToReadableString());
            }
        }
    }

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

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new NoopDisposable();

        public void Dispose()
        {
        }
    }

    #endregion
}
