namespace Application.Features.SyncTracking;

/// <summary>
/// Canonical run-item step names for sync execution stages.
/// Keep string-based to support composable category/stage naming.
/// </summary>
public static class SyncRunItemSteps
{
    /// <summary>
    /// Dispatch-stage run item name.
    /// </summary>
    public const string Dispatch = "Dispatch";

    /// <summary>
    /// Builds the references page-fetch run-item step name for the supplied category.
    /// </summary>
    public static string ReferencesPageFetch(string category)
    {
        return $"References:{category}:PageFetch";
    }

    /// <summary>
    /// Builds the references summary run-item step name for the supplied category.
    /// </summary>
    public static string ReferencesSummary(string category)
    {
        return $"References:{category}:Summary";
    }

    /// <summary>
    /// Builds the analytics page-fetch run-item step name for the supplied category.
    /// </summary>
    public static string AnalyticsPageFetch(string category)
    {
        return $"Analytics:{category}:PageFetch";
    }

    /// <summary>
    /// Builds the analytics summary run-item step name for the supplied category.
    /// </summary>
    public static string AnalyticsSummary(string category)
    {
        return $"Analytics:{category}:Summary";
    }

    /// <summary>
    /// Builds the canonical analytics page cursor for the supplied one-based page number.
    /// Zero-padded formatting preserves numeric ordering when cursors are compared lexically.
    /// </summary>
    /// <param name="pageNumber">One-based page number.</param>
    /// <returns>The canonical page cursor string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="pageNumber"/> is less than 1.
    /// </exception>
    public static string AnalyticsPageCursor(int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber),
                                                  pageNumber,
                                                  "Page number must be greater than or equal to 1.");
        }

        return $"page:{pageNumber:D8}";
    }

    /// <summary>
    /// Parses a canonical analytics page cursor back to its one-based page number.
    /// </summary>
    /// <param name="cursor">Canonical page cursor string.</param>
    /// <returns>The parsed one-based page number.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cursor"/> is empty, malformed, or does not contain a valid page number.
    /// </exception>
    public static int ParseAnalyticsPageCursor(string cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            throw new ArgumentException("Cursor is required.", nameof(cursor));
        }

        const string prefix = "page:";
        string trimmed = cursor.Trim();

        if (!trimmed.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Cursor must start with 'page:'.", nameof(cursor));
        }

        string numericPart = trimmed[prefix.Length..];
        if (!int.TryParse(numericPart, out int pageNumber) || pageNumber < 1)
        {
            throw new ArgumentException("Cursor must contain a valid page number.", nameof(cursor));
        }

        return pageNumber;
    }
}
