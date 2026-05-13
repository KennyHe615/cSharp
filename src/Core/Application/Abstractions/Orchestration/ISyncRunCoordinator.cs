namespace Application.Abstractions.Orchestration;

/// <summary>
/// Coordinates sync run lifecycle operations used by orchestration flows.
/// </summary>
public interface ISyncRunCoordinator
{
    /// <summary>
    /// Starts a new run for the specified sync request.
    /// Existing active run for the same request (if any) is superseded.
    /// </summary>
    /// <param name="requestId">Parent sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Newly created run id.</returns>
    Task<long> StartNewRunAsync(long requestId, CancellationToken ct);

    /// <summary>
    /// Starts a new run for the specified sync request when no active current run exists,
    /// otherwise returns the existing active current run id.
    /// </summary>
    /// <param name="requestId">Parent sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active run id, either newly created or already current.</returns>
    Task<long> StartOrJoinActiveRunAsync(long requestId, CancellationToken ct);

    /// <summary>
    /// Checks whether the run is still current and active for its request.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the run is current and active; otherwise <c>false</c>.</returns>
    Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks the run as completed.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCompletedAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks the run as completed while indicating recovery items were emitted.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCompletedWithRecoveryItemsAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Marks the run as failed.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="reason">Failure reason text.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkFailedAsync(long runId, string reason, CancellationToken ct);

    /// <summary>
    /// Marks the run as superseded by a newer run.
    /// </summary>
    /// <param name="runId">Run id being superseded.</param>
    /// <param name="supersededByRunId">Newer run id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct);

    /// <summary>
    /// Marks the run as canceled.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="reason">Optional cancellation reason.</param>
    /// <param name="ct">Cancellation token.</param>
    Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct);
}
