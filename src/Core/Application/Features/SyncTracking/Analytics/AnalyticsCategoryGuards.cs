using Application.Enums;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Provides shared guard logic for analytics sync categories supported by analytics workflows.
/// </summary>
internal static class AnalyticsCategoryGuards
{
    /// <summary>
    /// Determines whether the specified analytics category is supported by analytics sync processing.
    /// </summary>
    /// <param name="category">Analytics category to evaluate.</param>
    /// <returns><c>true</c> when <paramref name="category"/> is a supported analytics category; otherwise <c>false</c>.</returns>
    internal static bool IsAnalyticsCategory(SyncAnalyticsCategory category)
    {
        return category is SyncAnalyticsCategory.UsersDetails or SyncAnalyticsCategory.ConversationsDetails
                                                              or SyncAnalyticsCategory.ConversationsAggregates;
    }
}
