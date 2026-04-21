using Application.Enums;
using Application.Mediator;

namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Command to run an incremental sync for one analytics category scope.
/// </summary>
/// <param name="Category">Analytics category to execute.</param>
/// <param name="Interval">
/// UTC time window identifier for analytics incremental sync.
/// </param>
/// <param name="PageNumber">
/// Optional page number when the analytics category supports paged processing.
/// </param>
public sealed record RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory Category,
                                                        string? Interval,
                                                        int? PageNumber) : IRequest<long>;
