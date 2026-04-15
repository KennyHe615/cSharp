namespace Application.Features.SyncTracking.Shared;

/// <summary>
/// Canonical checkpoint step names for sync execution stages.
/// Keep string-based to support composable category/stage naming.
/// </summary>
public static class SyncCheckpointSteps
{
    public const string Dispatch = "Dispatch";

    public static string ReferencesPageFetch(string category)
    {
        return $"References:{category}:PageFetch";
    }

    public static string ReferencesSummary(string category)
    {
        return $"References:{category}:Summary";
    }

    public static string AnalyticsPageFetch(string category)
    {
        return $"Analytics:{category}:PageFetch";
    }

    public static string AnalyticsSummary(string category)
    {
        return $"Analytics:{category}:Summary";
    }
}
