using System.Text;
using System.Text.RegularExpressions;


namespace Shared.Extensions;

public static partial class StringExtensions
{
    public static string? ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Preserve leading underscores exactly as-is.
        int i = 0;
        while (i < input.Length && input[i] == '_')
        {
            i++;
        }

        if (i == input.Length)
        {
            return input; // all underscores
        }

        StringBuilder sb = new(input.Length + 8);

        // Copy prefix underscores
        if (i > 0) sb.Append(input, 0, i);

        for (; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '_')
            {
                // Keep underscores; avoid doubling.
                if (sb.Length == 0 || sb[^1] != '_')
                {
                    sb.Append('_');
                }

                continue;
            }

            if (char.IsUpper(c))
            {
                bool hasPrev = sb.Length > 0;
                bool prevIsUnderscore = hasPrev && sb[^1] == '_';

                // Look at neighbors in the original input (not the output).
                char prev = i > 0 ? input[i - 1] : '\0';
                char next = i + 1 < input.Length ? input[i + 1] : '\0';

                bool prevIsLowerOrDigit = i > 0 && (char.IsLower(prev) || char.IsDigit(prev));
                bool prevIsUpper = i > 0 && char.IsUpper(prev);
                bool nextIsLower = i + 1 < input.Length && char.IsLower(next);

                // Insert underscore at:
                // - lower/digit -> upper (e.g., myValue)
                // - acronym boundary: upper -> upper + next lower (e.g., HTTPServer => http_server)
                if (!prevIsUnderscore && hasPrev && (prevIsLowerOrDigit || (prevIsUpper && nextIsLower)))
                {
                    sb.Append('_');
                }

                sb.Append(char.ToLowerInvariant(c));

                continue;
            }

            // Lowercase letters, digits, etc.
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    public static string? Truncate(this string? value, int maxLength)
    {
        return value?.Length > maxLength ? value[..maxLength] : value;
    }

    /// <summary>
    /// Attempts to parse a string into a <see cref="Guid"/>.
    /// </summary>
    /// <param name="value">The string value to parse.</param>
    /// <returns>A <see cref="Guid"/> if parsing succeeds; otherwise, <see langword="null"/>.</returns>
    public static Guid? ToGuid(this string? value)
    {
        return Guid.TryParse(value, out Guid guid) ? guid : null;
    }

    public static string NormalizeSecretName(this string? secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name must be provided.", nameof(secretName));
        }

        // Azure Key Vault secret name rules (practical normalization):
        // - Allowed characters: a-z, A-Z, 0-9 and '-'
        // - Must start/end with alphanumeric
        // - Length: 1..127
        // This normalization:
        // - trims, lowercases
        // - converts whitespace/underscores to '-'
        // - removes other invalid characters
        // - collapses repeated '-'
        // - trims '-' from ends
        // - enforces length and boundary rules

        ReadOnlySpan<char> input = secretName.Trim().AsSpan();

        StringBuilder sb = new(input.Length);
        bool previousWasHyphen = false;

        foreach (char ch in input)
        {
            char c = char.ToLowerInvariant(ch);

            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                previousWasHyphen = false;

                continue;
            }

            if (c is not ('-' or '_') && !char.IsWhiteSpace(c)) continue;

            if (previousWasHyphen || sb.Length == 0) continue;

            sb.Append('-');
            previousWasHyphen = true;

            // Drop any other character.
        }

        string normalized = sb.ToString().Trim('-');

        switch (normalized.Length)
        {
            case 0:
                throw new ArgumentException("Secret name does not contain any valid characters after normalization.",
                                            nameof(secretName));
            case > 127:
                normalized = normalized[..127].Trim('-');

                break;
        }

        if (normalized.Length == 0 || !char.IsLetterOrDigit(normalized[0]) || !char.IsLetterOrDigit(normalized[^1]))
        {
            throw new ArgumentException(
                "Secret name must start and end with an alphanumeric character after normalization.",
                nameof(secretName));
        }

        return normalized;
    }

    #region ========== *** Private Methods *** ==========

    [GeneratedRegex("^_+")]
    private static partial Regex SnakeCaseRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseRegex2();

    #endregion
}
