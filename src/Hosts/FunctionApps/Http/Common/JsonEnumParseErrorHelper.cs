using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace FunctionApps.Http.Common;

/// <summary>
/// Builds user-friendly enum validation messages from <see cref="JsonException"/> path information.
/// </summary>
public static class JsonEnumParseErrorHelper
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> PropertyMaps =
                    new ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>>();

    /// <summary>
    /// Attempts to build an enum parse error message for the failing JSON field.
    /// </summary>
    /// <typeparam name="TRequest">Request model type used for property resolution.</typeparam>
    /// <param name="ex">The JSON exception raised during deserialization.</param>
    /// <param name="message">
    /// When this method returns <c>true</c>, contains a user-friendly error message;
    /// otherwise an empty string.
    /// </param>
    /// <returns>
    /// <c>true</c> when the exception path resolves to an enum property; otherwise <c>false</c>.
    /// </returns>
    public static bool TryBuildMessage<TRequest>(JsonException ex, out string message)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(ex.Path)) return false;

        string jsonPath = ex.Path.Trim();

        // Supports paths like "$.category" and nested "$.a.b.c" (and indexed tokens like "$.a[0].b")
        if (!jsonPath.StartsWith("$.", StringComparison.Ordinal)) return false;

        string relativePath = jsonPath[2..];
        PropertyInfo? property = ResolvePropertyByPath(typeof(TRequest), relativePath);

        if (property is null) return false;

        Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (!propertyType.IsEnum) return false;

        string fieldName = GetLastPathToken(relativePath);
        string[] allowed = Enum.GetNames(propertyType);

        message = $"Invalid value for '{fieldName}'. Available values: {string.Join(" / ", allowed)}.";

        return true;
    }

    /// <summary>
    /// Attempts to build an unsupported-field message from JSON exception path information.
    /// </summary>
    /// <typeparam name="TRequest">Request model type used for property resolution.</typeparam>
    /// <param name="ex">The JSON exception raised during deserialization.</param>
    /// <param name="message">
    /// When this method returns <c>true</c>, contains a user-friendly unsupported-field message;
    /// otherwise an empty string.
    /// </param>
    /// <returns><c>true</c> when the exception path identifies an unsupported field; otherwise <c>false</c>.</returns>
    public static bool TryBuildUnsupportedFieldMessage<TRequest>(JsonException ex, out string message)
    {
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(ex.Path)) return false;

        string jsonPath = ex.Path.Trim();

        if (!jsonPath.StartsWith("$.", StringComparison.Ordinal)) return false;

        string relativePath = jsonPath[2..];

        if (ResolvePropertyByPath(typeof(TRequest), relativePath) is not null) return false;

        string fieldName = GetLastPathToken(relativePath);

        if (string.IsNullOrWhiteSpace(fieldName)) return false;

        message = $"Unsupported field '{fieldName}'.";

        return true;
    }

    #region ========== *** Private Methods *** ==========

    private static PropertyInfo? ResolvePropertyByPath(Type rootType, string relativePath)
    {
        string[] rawTokens =
                        relativePath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Type currentType = rootType;
        PropertyInfo? currentProperty = null;

        foreach (string rawToken in rawTokens)
        {
            string token = StripIndexer(rawToken);

            if (string.IsNullOrWhiteSpace(token)) return null;

            IReadOnlyDictionary<string, PropertyInfo> propertyMap = GetPropertyMap(currentType);

            if (!propertyMap.TryGetValue(token, out currentProperty)) return null;

            currentType = Nullable.GetUnderlyingType(currentProperty.PropertyType) ?? currentProperty.PropertyType;
        }

        return currentProperty;
    }

    private static IReadOnlyDictionary<string, PropertyInfo> GetPropertyMap(Type type)
    {
        return PropertyMaps.GetOrAdd(type,
                                     t =>
                                     {
                                         Dictionary<string, PropertyInfo> map =
                                                         new Dictionary<string, PropertyInfo>(StringComparer
                                                                        .OrdinalIgnoreCase);

                                         foreach (PropertyInfo property in t.GetProperties(BindingFlags.Public
                                                      | BindingFlags.Instance))
                                         {
                                             map[property.Name] = property;

                                             JsonPropertyNameAttribute? jsonName =
                                                             property.GetCustomAttribute<JsonPropertyNameAttribute>();
                                             if (!string.IsNullOrWhiteSpace(jsonName?.Name))
                                             {
                                                 map[jsonName.Name] = property;
                                             }
                                         }

                                         return map;
                                     });
    }

    private static string GetLastPathToken(string relativePath)
    {
        string token = relativePath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                   .LastOrDefault()
                       ?? relativePath;

        return StripIndexer(token);
    }

    private static string StripIndexer(string token)
    {
        int bracketIndex = token.IndexOf('[');

        return bracketIndex >= 0 ? token[..bracketIndex] : token;
    }

    #endregion
}
