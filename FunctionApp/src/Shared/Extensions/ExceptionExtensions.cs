using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Shared.Extensions;

public static class ExceptionExtensions
{
    private static readonly JsonSerializerOptions LogJsonOptions = new()
                                                                   {
                                                                       WriteIndented = true,
                                                                       DefaultIgnoreCondition =
                                                                           JsonIgnoreCondition.WhenWritingNull
                                                                   };

    private static readonly string[] ExceptionPropertyNames = typeof(Exception)
                                                              .GetProperties(
                                                                  BindingFlags.Public | BindingFlags.Instance)
                                                              .Select(p => p.Name)
                                                              .ToArray();

    /// <summary>
    /// Console-friendly, single-line summary.
    /// </summary>
    public static string ToSummary(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        static string ShortType(Exception e)
        {
            return e.GetType().FullName ?? e.GetType().Name;
        }

        // Keep it compact; message may contain newlines.
        string msg = (ex.Message ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        if (msg.Length > 400) msg = msg[..400] + "...";

        // Include root-cause type if nested.
        Exception root = ex.GetRootCause();
        if (!ReferenceEquals(root, ex))
        {
            string rootMsg = (root.Message ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            if (rootMsg.Length > 200) rootMsg = rootMsg[..200] + "...";

            return $"{ShortType(ex)}: {msg} | Root: {ShortType(root)}: {rootMsg}";
        }

        return $"{ShortType(ex)}: {msg}";
    }

    /// <summary>
    /// Safe JSON (no raw Exception serialization, no MethodBase, etc.).
    /// Prefer logging the exception via ILogger and attach this JSON as a string when needed.
    /// </summary>
    public static string ToJson(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        ExceptionLogDto dto = ExceptionLogDto.From(ex);

        return JsonSerializer.Serialize(dto, LogJsonOptions);
    }

    /// <summary>
    /// Structured properties for logging scopes or telemetry properties.
    /// Use with: using(logger.BeginScope(ex.ToLogScope())) { logger.LogError(ex, "..."); }
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ToLogScope(this Exception ex, string prefix = "exception")
    {
        ArgumentNullException.ThrowIfNull(ex);
        if (string.IsNullOrWhiteSpace(prefix)) prefix = "exception";

        ExceptionLogDto dto = ExceptionLogDto.From(ex);

        // Flatten a few key fields for easy querying in Application Insights.
        return new Dictionary<string, object?>
               {
                   [$"{prefix}.type"] = dto.Type,
                   [$"{prefix}.message"] = dto.Message,
                   [$"{prefix}.hresult"] = dto.HResult,
                   [$"{prefix}.source"] = dto.Source,
                   [$"{prefix}.stackTrace"] = dto.StackTrace,
                   [$"{prefix}.activityId"] = dto.ActivityId,
                   [$"{prefix}.inner.type"] = dto.Inner?.Type,
                   [$"{prefix}.inner.message"] = dto.Inner?.Message,
                   [$"{prefix}.json"] = JsonSerializer.Serialize(dto, LogJsonOptions)
               };
    }

    public static Exception GetRootCause(this Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        Exception current = ex;

        // If AggregateException: unwrap to the first inner by default.
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

    // ---- Internal safe DTO shape ----

    private sealed record ExceptionLogDto(string Type,
                                          string Message,
                                          string? Source,
                                          int HResult,
                                          string? StackTrace,
                                          string? ActivityId,
                                          IReadOnlyDictionary<string, string?>? Data,
                                          IReadOnlyList<ExceptionLogDto>? InnerExceptions,
                                          ExceptionLogDto? Inner)
    {
        public static ExceptionLogDto From(Exception ex)
        {
            // Note: don’t touch ex.TargetSite (MethodBase) or any reflection-y properties.
            IReadOnlyDictionary<string, string?>? data = TryExtractData(ex);

            if (ex is AggregateException agg)
            {
                AggregateException flat = agg.Flatten();
                List<ExceptionLogDto> inners = flat.InnerExceptions.Select(From).ToList();

                return new ExceptionLogDto(ex.GetType().FullName ?? ex.GetType().Name,
                                           ex.Message ?? string.Empty,
                                           ex.Source,
                                           ex.HResult,
                                           ex.StackTrace,
                                           Activity.Current?.Id,
                                           data,
                                           inners,
                                           ex.InnerException is null ? null : From(ex.InnerException));
            }

            return new ExceptionLogDto(ex.GetType().FullName ?? ex.GetType().Name,
                                       ex.Message ?? string.Empty,
                                       ex.Source,
                                       ex.HResult,
                                       ex.StackTrace,
                                       Activity.Current?.Id,
                                       data,
                                       null,
                                       ex.InnerException is null ? null : From(ex.InnerException));
        }

        private static IReadOnlyDictionary<string, string?>? TryExtractData(Exception ex)
        {
            try
            {
                if (ex.Data is null || ex.Data.Count == 0)
                {
                    return null;
                }

                // Convert Data keys/values to strings only (safe to serialize and query).
                Dictionary<string, string?> dict = new(StringComparer.OrdinalIgnoreCase);

                foreach (DictionaryEntry entry in ex.Data)
                {
                    string key = entry.Key?.ToString() ?? "(null)";
                    string? value = entry.Value?.ToString();
                    dict[key] = value;
                }

                return dict;
            }
            catch
            {
                // Never let formatting throw.
                return null;
            }
        }
    }
}
