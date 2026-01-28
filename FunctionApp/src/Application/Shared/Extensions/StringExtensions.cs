using System.Text.RegularExpressions;


namespace Application.Shared.Extensions;

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

    #region ========== *** Private Methods *** ==========

    [GeneratedRegex("^_+")]
    private static partial Regex SnakeCaseRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseRegex2();

    #endregion
}
