namespace Application.Features.SyncTracking.Shared;

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
}
