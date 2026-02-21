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
