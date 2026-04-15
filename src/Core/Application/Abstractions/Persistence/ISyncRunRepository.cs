using Application.DTOs.SyncTracking;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for physical sync run lifecycle management.
/// A run represents one concrete execution attempt for a single sync request scope.
/// </summary>
public interface ISyncRunRepository
{
    /// <summary>
    /// Starts a new run for the specified request.
    /// If an active run (<c>Pending</c> or <c>Running</c>) already exists, it is superseded first.
    /// </summary>
    /// <param name="requestId">Parent sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created run id.</returns>
    Task<long> StartNewRunAsync(long requestId, CancellationToken ct);

    /// <summary>
    /// Checks whether the specified run is still the current active run for its request.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when current and active; otherwise <c>false</c>.</returns>
    Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks an active run as completed.
    /// No-op when the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCompletedAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks an active run as failed and stores a run-level failure summary.
    /// No-op when the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="reason">Failure detail used to derive the run-level summary.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkFailedAsync(long runId, string reason, CancellationToken ct);

    /// <summary>
    /// Marks an active run as superseded by a newer run id.
    /// No-op when the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id being superseded.</param>
    /// <param name="supersededByRunId">Newer run id that superseded this run.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct);

    /// <summary>
    /// Marks an active run as canceled and stores a run-level cancellation summary.
    /// No-op when the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="reason">Optional cancellation detail used to derive the run-level summary.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct);
}
