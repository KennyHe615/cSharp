using System.Text;


namespace SharedKernel.Extensions;

/// <summary>
/// String helper extensions used across the application.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts the input to <c>snake_case</c>.
    /// </summary>
    /// <param name="input">The source value to transform.</param>
    /// <returns>
    /// The <c>snake_case</c> result, or the original value when <paramref name="input"/> is
    /// <c>null</c> or empty.
    /// </returns>
    /// <remarks>
    /// Preserves leading underscores, avoids duplicate underscores, and supports acronym boundaries
    /// (for example, <c>HTTPServer</c> becomes <c>http_server</c>).
    /// </remarks>
    public static string? ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Fast-path: already lowercase snake_case (ascii letters/digits/underscore only).
        bool isAlreadySnake = input.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '_');

        if (isAlreadySnake) return input;

        int i = 0;
        while (i < input.Length && input[i] == '_')
        {
            i++;
        }

        StringBuilder sb = new StringBuilder(input.Length + 8);

        if (i > 0) sb.Append(input, 0, i);// preserve leading underscores

        for (; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '_')
            {
                if (sb.Length == 0 || sb[^1] != '_') sb.Append('_');

                continue;
            }

            if (char.IsUpper(c))
            {
                bool hasPrevOut = sb.Length > 0;
                bool prevOutIsUnderscore = hasPrevOut && sb[^1] == '_';

                char prevIn = i     > 0 ? input[i            - 1] : '\0';
                char nextIn = i + 1 < input.Length ? input[i + 1] : '\0';

                bool prevIsLowerOrDigit = i > 0            && (char.IsLower(prevIn) || char.IsDigit(prevIn));
                bool prevIsUpper = i        > 0            && char.IsUpper(prevIn);
                bool nextIsLower = i + 1    < input.Length && char.IsLower(nextIn);

                // boundary from uncased letter scripts (e.g., 加拿大 + Value => 加拿大_value)
                bool prevIsLetterWithoutCase =
                    i > 0 && char.IsLetter(prevIn) && !char.IsLower(prevIn) && !char.IsUpper(prevIn);

                if (!prevOutIsUnderscore
                    && hasPrevOut
                    && (prevIsLowerOrDigit || prevIsUpper && nextIsLower || prevIsLetterWithoutCase))
                {
                    sb.Append('_');
                }
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Converts the input to uppercase snake case.
    /// </summary>
    /// <param name="input">The source value to transform.</param>
    /// <returns>
    /// The uppercase snake-case result, or the original value when <paramref name="input"/> is
    /// <see langword="null"/> or empty.
    /// </returns>
    public static string? ToSnakeUpperCase(this string? input)
    {
        return input?.ToSnakeCase()?.ToUpperInvariant();
    }

    /// <summary>
    /// Truncates a string to the specified maximum length.
    /// </summary>
    /// <param name="value">The source value.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <returns>
    /// The truncated value when it exceeds <paramref name="maxLength"/>; otherwise the original value.
    /// Returns <c>null</c> when <paramref name="value"/> is <c>null</c>.
    /// </returns>
    public static string? Truncate(this string? value, int maxLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        return value?.Length > maxLength ? value[..maxLength] : value;
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>A <see cref="Guid"/> if parsing succeeds; otherwise, <c>null</c>.</returns>
    public static Guid? ToGuid(this string? value)
    {
        return Guid.TryParse(value, out Guid guid) ? guid : null;
    }
}
