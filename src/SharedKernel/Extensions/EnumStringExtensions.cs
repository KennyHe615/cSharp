using System.Text;


namespace SharedKernel.Extensions;

public static class EnumStringExtensions
{
    public static TEnum ReadEnum<TEnum>(this string? value)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Empty value for {typeof(TEnum).Name}.", nameof(value));
        }

        string token = value.NormalizeEnumToken();

        return EnumMap<TEnum>.TokenToEnum.TryGetValue(token, out TEnum parsed)
            ? parsed
            : throw new ArgumentException($"Unknown {typeof(TEnum).Name} value: {value}", nameof(value));
    }

    /// <summary>
    /// Converts an enum value to canonical <c>SNAKE_UPPER</c>.
    /// </summary>
    public static string WriteEnumSnakeUpper<TEnum>(this TEnum value)
        where TEnum : struct, Enum
    {
        string name = value.ToString();

        return name.ToSnakeUpperCase() ?? name.ToUpperInvariant();
    }

    /// <summary>
    /// Normalizes an enum token by removing separators and converting to uppercase.
    /// </summary>
    /// <param name="value">The raw enum token.</param>
    /// <returns>The normalized token.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when the normalized token is empty.</exception>
    /// <remarks>
    /// Underscores, hyphens, and whitespace are removed.
    /// </remarks>
    public static string NormalizeEnumToken(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        StringBuilder sb = new StringBuilder(value.Length);

        foreach (char ch in value.Where(ch => ch != '_' && ch != '-' && !char.IsWhiteSpace(ch)))
        {
            sb.Append(char.ToUpperInvariant(ch));
        }

        return sb.Length == 0
            ? throw new ArgumentException("Enum token cannot be empty after normalization.", nameof(value))
            : sb.ToString();
    }

    #region ========== *** Private Class *** ==========

    private static class EnumMap<TEnum>
        where TEnum : struct, Enum
    {
        public static readonly IReadOnlyDictionary<string, TEnum> TokenToEnum = BuildMap();

        private static Dictionary<string, TEnum> BuildMap()
        {
            Dictionary<string, TEnum> dict = new Dictionary<string, TEnum>(StringComparer.Ordinal);

            foreach (string name in Enum.GetNames<TEnum>())
            {
                string token = name.NormalizeEnumToken();
                TEnum enumValue = Enum.Parse<TEnum>(name);

                if (dict.TryGetValue(token, out TEnum existing))
                {
                    throw new
                        InvalidOperationException($"Enum normalization collision in {typeof(TEnum).Name}: '{existing}' and '{name}' both normalize to '{token}'.");
                }

                dict[token] = enumValue;
            }

            return dict;
        }
    }

    #endregion
}
