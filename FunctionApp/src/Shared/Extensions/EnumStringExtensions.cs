using System.Collections.Concurrent;


namespace Shared.Extensions;

public static class EnumStringExtensions
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, object>> TokenToEnumCache = new();

    public static TEnum ReadEnum<TEnum>(this string value) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Empty value for {typeof(TEnum).Name}.", nameof(value));
        }

        string token = value.NormalizeEnumToken();

        Dictionary<string, object> map = TokenToEnumCache.GetOrAdd(typeof(TEnum), _ => BuildMap<TEnum>());

        if (map.TryGetValue(token, out object? boxed) && boxed is TEnum typed)
        {
            return typed;
        }

        throw new ArgumentException($"Unknown {typeof(TEnum).Name} value: {value}", nameof(value));
    }

    public static bool TryReadEnum<TEnum>(this string value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value)) return false;

        string token = value.NormalizeEnumToken();
        Dictionary<string, object> map = TokenToEnumCache.GetOrAdd(typeof(TEnum), _ => BuildMap<TEnum>());

        if (!map.TryGetValue(token, out object? boxed) || boxed is not TEnum typed) return false;

        result = typed;

        return true;
    }

    public static string WriteEnumSnakeUpper<TEnum>(this TEnum value) where TEnum : struct, Enum
    {
        return value.ToString().ToSnakeUpperCase()!;
    }

    #region ========== *** Private Methods *** ==========

    private static Dictionary<string, object> BuildMap<TEnum>() where TEnum : struct, Enum
    {
        Dictionary<string, object> dict = new(StringComparer.Ordinal);

        foreach (string name in Enum.GetNames<TEnum>())
        {
            string token = name.NormalizeEnumToken();
            dict[token] = Enum.Parse<TEnum>(name);
        }

        return dict;
    }

    #endregion
}
