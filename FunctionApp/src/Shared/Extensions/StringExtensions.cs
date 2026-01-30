using System.Text;
using System.Text.RegularExpressions;


namespace Shared.Extensions;

public static partial class StringExtensions
{
    public static string ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input!;
        }

        Match startUnderscores = SnakeCaseRegex().Match(input);

        return startUnderscores +
               SnakeCaseRegex2().Replace(input[startUnderscores.Length..], "$1_$2").ToLowerInvariant();
    }

    public static string? Truncate(this string? value, int maxLength)
    {
        return value?.Length > maxLength ? value[..maxLength] : value;
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

        ReadOnlySpan<char> input = secretName.Trim();

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
