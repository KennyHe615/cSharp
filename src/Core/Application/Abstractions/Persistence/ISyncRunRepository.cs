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
    /// If an active run (Pending/Running) already exists for that request, it is superseded first.
    /// </summary>
    /// <param name="requestId">Parent sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created run id.</returns>
    Task<long> StartNewRunAsync(long requestId, CancellationToken ct);

    /// <summary>
    /// Gets one run by id.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="SyncRunDto"/> when found; otherwise <c>null</c>.</returns>
    Task<SyncRunDto?> GetByIdAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Checks whether the provided run id is still the current active run
    /// (CurrentRunId on request points to this run and run status is Pending/Running).
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when current and active; otherwise <c>false</c>.</returns>
    Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks an active run as Completed.
    /// No-op if the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCompletedAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks an active run as Failed and stores a bounded failure reason.
    /// No-op if the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="reason">Failure reason text.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkFailedAsync(long runId, string reason, CancellationToken ct);

    /// <summary>
    /// Marks an active run as Superseded by a newer run id.
    /// No-op if the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id being superseded.</param>
    /// <param name="supersededByRunId">Newer run id that superseded this run.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct);

    /// <summary>
    /// Marks an active run as Canceled and stores optional reason.
    /// No-op if the run is already finalized.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct);
}
