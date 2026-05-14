using Application.Abstractions.Orchestration.Sync;
using Application.Features.SyncTracking.Analytics;


namespace Application.Abstractions.Orchestration.Analytics;

/// <summary>
/// Coordinates shared page-level execution behavior for analytics categories.
/// Category-specific executors provide page resolution and page processing callbacks.
/// </summary>
public interface IAnalyticsPageSyncCoordinator
{
    /// <summary>
    /// Executes a page-level analytics sync workflow with durable run-item claims and recovery creation.
    /// </summary>
    /// <param name="request">Page-level analytics sync request.</param>
    /// <param name="ct">Cancellation token propagated by the sync runner.</param>
    /// <returns>The sync execution result for the page workflow.</returns>
    Task<SyncExecutionResult> ExecuteAsync(AnalyticsPageSyncRequest request, CancellationToken ct);
}
