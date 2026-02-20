using System.Collections;
using System.Diagnostics;
using System.Text;


namespace SharedKernel.Logging;

/// <summary>
/// Provides logging-focused extension methods for <see cref="Exception"/> to produce
/// structured scope data, concise summaries, root-cause extraction, and human-readable output.
/// </summary>
public static class ExceptionLoggingExtensions
{
    private const int SummaryMax = 500;
    private const int RootSummaryMax = 200;
    private const int MaxDepth = 8;
    private const int MaxStackLines = 800;
    private const int MaxDataItems = 20;

    /// <summary>
    /// Builds a concise, single-line summary for an exception, including root-cause details when present.
    /// </summary>
    /// <param name="ex">The exception to summarize.</param>
    /// <returns>A compact summary suitable for warning/error log lines.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is <c>null</c>.</exception>
    public static string ToSummary(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        string msg = Normalize(ex.Message, SummaryMax);
        Exception root = ex.GetRootCause();

        if (ReferenceEquals(root, ex)) return $"{ShortType(ex)}: {msg}";

        string rootMsg = Normalize(root.Message, RootSummaryMax);

        return $"{ShortType(ex)}: {msg} | Root: {ShortType(root)}: {rootMsg}";
    }

    /// <summary>
    /// Converts an exception into a structured dictionary suitable for logging scopes.
    /// </summary>
    /// <param name="ex">The exception to convert.</param>
    /// <param name="prefix">
    /// Key prefix used for generated properties (for example: <c>exception.type</c>, <c>exception.message</c>).
    /// </param>
    /// <returns>A read-only dictionary containing normalized exception metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null, empty, or whitespace.</exception>
    public static IReadOnlyDictionary<string, object?> ToLogScope(this Exception ex, string prefix = "exception")
    {
        ArgumentNullException.ThrowIfNull(ex);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        Exception root = ex.GetRootCause();

        Dictionary<string, object?> scope = new Dictionary<string, object?>(StringComparer.Ordinal)
                                            {
                                                [$"{prefix}.type"] = ShortType(ex),
                                                [$"{prefix}.message"] = ex.Message,
                                                [$"{prefix}.hresult"] = ex.HResult
                                            };

        if (!string.IsNullOrWhiteSpace(ex.Source)) scope[$"{prefix}.source"] = ex.Source;

        if (Activity.Current?.Id is {} activityId) scope[$"{prefix}.activity_id"] = activityId;

        if (ReferenceEquals(root, ex)) return scope;

        scope[$"{prefix}.root_type"] = ShortType(root);
        scope[$"{prefix}.root_message"] = root.Message;

        return scope;
    }

    /// <summary>
    /// Returns the deepest/primary root cause by traversing inner exceptions.
    /// For <see cref="AggregateException"/>, the first flattened branch is treated as primary.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>The root-cause exception instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is <c>null</c>.</exception>
    public static Exception GetRootCause(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Exception current = ex;

        if (current is AggregateException agg)
        {
            AggregateException flat = agg.Flatten();

            if (flat.InnerExceptions.Count > 0) current = flat.InnerExceptions[0];
        }

        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    /// <summary>
    /// Renders a human-readable, multi-line exception report with optional stack traces.
    /// Output is bounded by depth and line limits to avoid unbounded log volume.
    /// </summary>
    /// <param name="ex">The exception to render.</param>
    /// <param name="includeStackTrace">
    /// <see langword="true"/> to include stack traces; otherwise only metadata and hierarchy are included.
    /// </param>
    /// <returns>A formatted exception report string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is <c>null</c>.</exception>
    public static string ToReadableString(this Exception ex, bool includeStackTrace = true)
    {
        ArgumentNullException.ThrowIfNull(ex);

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("EXCEPTION DETAILS");
        sb.AppendLine("┌─────────────────────────────────────────────────────────────");

        Append(sb,
               ex,
               includeStackTrace,
               0);

        sb.AppendLine("└─────────────────────────────────────────────────────────────");

        return sb.ToString();
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Recursively appends exception details with indentation, handling inner and aggregate exceptions.
    /// </summary>
    private static void Append(StringBuilder sb, Exception ex, bool includeStackTrace, int depth)
    {
        while (true)
        {
            if (depth > MaxDepth)
            {
                sb.AppendLine($"{Indent(depth)}<max depth reached>");

                return;
            }

            string i = Indent(depth);
            sb.AppendLine($"{i}Type: {ShortType(ex)}");
            sb.AppendLine($"{i}Message: {ex.Message}");

            if (!string.IsNullOrWhiteSpace(ex.Source)) sb.AppendLine($"{i}Source: {ex.Source}");

            if (ex.HResult != 0) sb.AppendLine($"{i}HResult: {ex.HResult}");

            if (ex.Data.Count > 0)
            {
                sb.AppendLine($"{i}Data:");

                int count = 0;
                foreach (DictionaryEntry entry in ex.Data)
                {
                    if (count++ >= MaxDataItems)
                    {
                        sb.AppendLine($"{i}  <truncated>");

                        break;
                    }

                    sb.AppendLine($"{i}  - {entry.Key}: {entry.Value}");
                }
            }

            if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                sb.AppendLine($"{i}Stack:");

                string[] lines = ex.StackTrace.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries);

                for (int idx = 0; idx < lines.Length && idx < MaxStackLines; idx++)
                {
                    sb.AppendLine($"{i}  {lines[idx].Trim()}");
                }

                if (lines.Length > MaxStackLines) sb.AppendLine($"{i}  <stack truncated>");
            }

            // Aggregate path: list each branch once, do not also walk InnerException separately.
            if (ex is AggregateException { InnerExceptions.Count: > 1 } aggregate)
            {
                sb.AppendLine($"{i}Aggregate ({aggregate.InnerExceptions.Count}):");

                for (int k = 0; k < aggregate.InnerExceptions.Count; k++)
                {
                    sb.AppendLine($"{i}[{k + 1}]");

                    Append(sb,
                           aggregate.InnerExceptions[k],
                           includeStackTrace,
                           depth + 1);
                }

                return;
            }

            // Non-aggregate (or single-inner aggregate) path.
            if (ex.InnerException is null) return;

            sb.AppendLine($"{i}Inner:");
            ex = ex.InnerException;
            depth += 1;
        }
    }

    /// <summary>
    /// Normalizes line breaks and truncates message content to a maximum length.
    /// </summary>
    private static string Normalize(string value, int max)
    {
        string normalized = value.Replace("\r", " ").Replace("\n", " ").Trim();

        return normalized.Length <= max ? normalized : normalized[..max] + "...";
    }

    /// <summary>
    /// Returns full type name when available; otherwise simple type name.
    /// </summary>
    private static string ShortType(Exception e)
    {
        return e.GetType().FullName ?? e.GetType().Name;
    }

    /// <summary>
    /// Returns indentation spaces for the given hierarchy depth.
    /// </summary>
    private static string Indent(int depth)
    {
        return new string(' ', depth * 2);
    }

    #endregion
}
