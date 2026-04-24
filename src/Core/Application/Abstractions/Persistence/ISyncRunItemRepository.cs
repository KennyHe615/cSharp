using Application.DTOs.SyncTracking;
using Application.Enums;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for claimable sync run items used by execution stages.
/// </summary>
public interface ISyncRunItemRepository
{
    /// <summary>
    /// Creates or updates a run item identified by (runId, step, cursor).
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage or item name.</param>
    /// <param name="cursor">Item cursor token, for example page number or slice id.</param>
    /// <param name="status">Run-item status.</param>
    /// <param name="failureReason">Optional failure reason for failed or canceled statuses.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(long runId,
                     string step,
                     string cursor,
                     SyncRunStatus status,
                     string? failureReason,
                     CancellationToken ct);

    /// <summary>
    /// Gets the latest completed run item for the specified run and step.
    /// Completed terminal states include <see cref="SyncRunStatus.Completed"/> and
    /// <see cref="SyncRunStatus.CompletedWithRecoveryItems"/>.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage or item name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest completed run item when found; otherwise <c>null</c>.</returns>
    Task<SyncRunItemDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct);

    /// <summary>
    /// Gets failed run items for the specified run, ordered by newest first.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Failed run-item collection.</returns>
    Task<IReadOnlyCollection<SyncRunItemDto>> GetFailedAsync(long runId, CancellationToken ct);
}
