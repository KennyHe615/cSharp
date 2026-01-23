using System.Text.RegularExpressions;


namespace FunctionApp.Infrastructure.Extensions;

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

    #region ========== *** Private Methods *** ==========

    [GeneratedRegex("^_+")]
    private static partial Regex SnakeCaseRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseRegex2();

    #endregion
}
