using Application.DTOs.SyncTracking;
using Application.Enums;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for run checkpoint records used by execution stages.
/// </summary>
public interface ISyncCheckpointRepository
{
    /// <summary>
    /// Creates or updates a checkpoint identified by (runId, step, cursor).
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage name.</param>
    /// <param name="cursor">Stage cursor token (for example page number or slice id).</param>
    /// <param name="status">Checkpoint status.</param>
    /// <param name="failureReason">Optional failure reason for failed/canceled statuses.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(long runId,
                     string step,
                     string cursor,
                     SyncRunStatus status,
                     string? failureReason,
                     CancellationToken ct);

    /// <summary>
    /// Gets the latest completed checkpoint for the specified run and step.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest completed checkpoint when found; otherwise <c>null</c>.</returns>
    Task<SyncCheckpointDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct);

    /// <summary>
    /// Gets failed checkpoints for the specified run, ordered by newest first.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Failed checkpoint collection.</returns>
    Task<IReadOnlyCollection<SyncCheckpointDto>> GetFailedAsync(long runId, CancellationToken ct);
}
