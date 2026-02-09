using Shared.Extensions;


namespace Infrastructure.Persistence.Mappers.Shared;

/// <summary>
/// Provides advanced extension methods for AutoMapper configurations to simplify common mapping patterns and handle dynamic data structures.
/// </summary>
public static class MappingExtensions
{
    /// <summary>
    /// Retrieves a value from a nested dictionary structure using dot-notation path traversal.
    /// </summary>
    /// <param name="dictionary">The dictionary to traverse. Can be <see langword="null"/>.</param>
    /// <param name="path">Dot-separated path to the target value (e.g., "division.id").</param>
    /// <returns>The string representation of the resolved value, or <see langword="null"/> if the path is invalid or the value does not exist.</returns>
    /// <remarks>
    /// This overload does not truncate the result. It is expression-tree friendly and suitable for use within AutoMapper configurations.
    /// </remarks>
    public static string? GetValue(this System.Collections.IDictionary? dictionary, string path)
    {
        return dictionary.GetValue(path, null);
    }

    /// <summary>
    /// Retrieves a value from a nested dictionary structure using dot-notation path traversal, with optional truncation.
    /// </summary>
    /// <param name="dictionary">The dictionary to traverse. Can be <see langword="null"/>.</param>
    /// <param name="path">Dot-separated path to the target value (e.g., "division.id").</param>
    /// <param name="truncate">Maximum length of the returned string. If <see langword="null"/>, no truncation is applied.</param>
    /// <returns>The string representation of the resolved value (truncated if specified), or <see langword="null"/> if the path is invalid or the value does not exist.</returns>
    /// <remarks>
    /// <para>
    /// Path segments are split by '.' and trimmed. Each segment must exist as a key in the current dictionary level.
    /// Traversal stops and returns <see langword="null"/> if any intermediate value is not a dictionary or a key is missing.
    /// </para>
    /// <para>
    /// Commonly used in AutoMapper profiles to extract nested properties from PureCloud API responses (e.g., "division.name").
    /// </para>
    /// </remarks>
    public static string? GetValue(this System.Collections.IDictionary? dictionary, string path, int? truncate)
    {
        if (dictionary is null) return null;
        if (string.IsNullOrWhiteSpace(path)) return null;

        object? current = dictionary;

        foreach (string key in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is not System.Collections.IDictionary dict) return null;
            if (!dict.Contains(key)) return null;

            current = dict[key];

            if (current is null) return null;
        }

        string? result = current.ToString();

        return truncate.HasValue ? result.Truncate(truncate.Value) : result;
    }
}
