using System.Collections;
using System.Diagnostics;
using System.Text;


namespace Shared.Extensions;

/// <summary>
/// Provides extension methods for enhanced exception handling and formatting.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Converts an exception to a concise summary string, including root cause information.
    /// </summary>
    /// <param name="ex">The exception to summarize.</param>
    /// <returns>A formatted summary string containing exception type, message, and root cause details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static string ToSummary(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        string msg = ex.Message.Replace("\r", " ").Replace("\n", " ").Trim();
        if (msg.Length > 400) msg = msg[..400] + "...";

        Exception root = ex.GetRootCause();

        if (ReferenceEquals(root, ex)) return $"{ShortType(ex)}: {msg}";

        string rootMsg = root.Message.Replace("\r", " ").Replace("\n", " ").Trim();
        if (rootMsg.Length > 200) rootMsg = rootMsg[..200] + "...";

        return $"{ShortType(ex)}: {msg} | Root: {ShortType(root)}: {rootMsg}";
    }

    /// <summary>
    /// Converts an exception to a detailed, human-readable string format with structured layout.
    /// </summary>
    /// <param name="ex">The exception to format.</param>
    /// <param name="includeStackTrace">Whether to include stack trace information in the output. Default is true.</param>
    /// <returns>A formatted multi-line string containing comprehensive exception details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static string ToReadableString(this Exception ex, bool includeStackTrace = true)
    {
        ArgumentNullException.ThrowIfNull(ex);

        StringBuilder sb = new();

        sb.AppendLine("┌─────────────────────────────────────────────────────────────");
        sb.AppendLine("│ EXCEPTION DETAILS");
        sb.AppendLine("├─────────────────────────────────────────────────────────────");

        AppendExceptionDetails(sb, ex, includeStackTrace);

        sb.AppendLine("└─────────────────────────────────────────────────────────────");

        return sb.ToString();
    }

    /// <summary>
    /// Converts an exception to a structured dictionary suitable for logging scopes.
    /// </summary>
    /// <param name="ex">The exception to convert.</param>
    /// <param name="prefix">The prefix to use for dictionary keys. Default is "Exception".</param>
    /// <returns>A read-only dictionary containing exception properties as key-value pairs.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static IReadOnlyDictionary<string, object?> ToLogScope(this Exception ex, string prefix = "Exception")
    {
        ArgumentNullException.ThrowIfNull(ex);

        Exception root = ex.GetRootCause();

        Dictionary<string, object?> scope = new(StringComparer.OrdinalIgnoreCase)
                                            {
                                                [$"{prefix}Type"] = ex.GetType().FullName ?? ex.GetType().Name,
                                                [$"{prefix}Message"] = ex.Message,
                                                [$"{prefix}HResult"] = ex.HResult
                                            };

        if (!string.IsNullOrWhiteSpace(ex.Source)) scope[$"{prefix}Source"] = ex.Source;

        if (Activity.Current?.Id != null) scope[$"{prefix}ActivityId"] = Activity.Current.Id;

        if (ReferenceEquals(root, ex)) return scope;

        scope[$"{prefix}RootType"] = root.GetType().FullName ?? root.GetType().Name;
        scope[$"{prefix}RootMessage"] = root.Message;

        return scope;
    }

    /// <summary>
    /// Retrieves the root cause exception by traversing the inner exception chain.
    /// Handles AggregateException by flattening and selecting the first inner exception.
    /// </summary>
    /// <param name="ex">The exception to analyze.</param>
    /// <returns>The root cause exception at the bottom of the exception chain.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="ex"/> is null.</exception>
    public static Exception GetRootCause(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Exception current = ex;

        if (current is AggregateException agg)
        {
            AggregateException flat = agg.Flatten();
            if (flat.InnerExceptions.Count > 0)
            {
                current = flat.InnerExceptions[0];
            }
        }

        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current;
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Recursively appends exception details to a StringBuilder with proper indentation.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="ex">The exception to process.</param>
    /// <param name="includeStackTrace">Whether to include stack trace information.</param>
    /// <param name="depth">Current recursion depth for indentation purposes.</param>
    private static void AppendExceptionDetails(StringBuilder sb, Exception ex, bool includeStackTrace, int depth = 0)
    {
        string indent = new(' ', depth * 2);
        string prefix = depth == 0 ? "│ " : $"│ {indent}↳ ";

        sb.AppendLine($"{prefix}Type: {ShortType(ex)}");
        sb.AppendLine($"{prefix}Message: {ex.Message}");

        if (!string.IsNullOrWhiteSpace(ex.Source)) sb.AppendLine($"{prefix}Source: {ex.Source}");

        if (ex.HResult != 0) sb.AppendLine($"{prefix}HResult: {ex.HResult}");

        if (Activity.Current?.Id != null) sb.AppendLine($"{prefix}ActivityId: {Activity.Current.Id}");

        if (ex.Data.Count > 0)
        {
            sb.AppendLine($"{prefix}Data:");

            foreach (DictionaryEntry entry in ex.Data)
            {
                sb.AppendLine($"{prefix}  - {entry.Key}: {entry.Value}");
            }
        }

        if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            sb.AppendLine($"{prefix}Stack Trace:");
            string[] lines = ex.StackTrace.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                sb.AppendLine($"{prefix}  {line.Trim()}");
            }
        }

        if (ex.InnerException != null)
        {
            sb.AppendLine($"{prefix}");
            sb.AppendLine($"{prefix}Inner Exception:");
            AppendExceptionDetails(sb, ex.InnerException, includeStackTrace, depth + 1);
        }

        if (ex is not AggregateException { InnerExceptions.Count: > 1 } aggregateEx) return;

        sb.AppendLine($"{prefix}");
        sb.AppendLine($"{prefix}Aggregate Exceptions ({aggregateEx.InnerExceptions.Count}):");
        for (int i = 0; i < aggregateEx.InnerExceptions.Count; i++)
        {
            sb.AppendLine($"{prefix}");
            sb.AppendLine($"{prefix}[{i + 1}]:");
            AppendExceptionDetails(sb, aggregateEx.InnerExceptions[i], includeStackTrace, depth + 1);
        }
    }

    /// <summary>
    /// Gets the short type name of an exception, preferring full name over simple name.
    /// </summary>
    /// <param name="e">The exception to get the type name from.</param>
    /// <returns>The full name or simple name of the exception type.</returns>
    private static string ShortType(Exception e)
    {
        return e.GetType().FullName ?? e.GetType().Name;
    }

    #endregion
}
