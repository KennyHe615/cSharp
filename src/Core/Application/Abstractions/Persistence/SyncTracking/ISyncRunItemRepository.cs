using Application.DTOs.SyncTracking;
using Application.Enums;


namespace Application.Abstractions.Persistence.SyncTracking;

/// <summary>
/// Persistence contract for claimable sync run items used by execution stages.
/// Supports both generic stage markers and page-level distributed claim workflows.
/// </summary>
public interface ISyncRunItemRepository
{
    /// <summary>
    /// Creates or updates a generic run item identified by <c>(runId, step, cursor)</c>.
    /// This overload is intended for non-page work items such as dispatch or summary markers.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage or item name.</param>
    /// <param name="cursor">Generic item cursor token, for example a scope key or slice id.</param>
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
    /// Seeds pending page work items for the supplied run and step.
    /// The operation must be idempotent so repeated calls do not create duplicate rows.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical page-work step name.</param>
    /// <param name="pageNumbers">Ordered one-based page numbers to seed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SeedPendingPagesAsync(long runId, string step, IReadOnlyCollection<int> pageNumbers, CancellationToken ct);

    /// <summary>
    /// Atomically claims the next eligible pending or expired page item for the supplied run and step.
    /// Returns <c>null</c> when no eligible page is available.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical page-work step name.</param>
    /// <param name="claimedBy">Logical worker identifier acquiring the lease.</param>
    /// <param name="leaseToken">Fresh lease ownership token generated for this claim attempt.</param>
    /// <param name="claimedAtEastern">Eastern application timestamp when the lease is acquired.</param>
    /// <param name="claimExpiresAtEastern">Eastern application timestamp when the lease will expire.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claimed page item projection when a claim succeeds; otherwise <c>null</c>.</returns>
    Task<SyncRunItemDto?> ClaimNextPageAsync(long runId,
                                             string step,
                                             string claimedBy,
                                             Guid leaseToken,
                                             DateTimeOffset claimedAtEastern,
                                             DateTimeOffset claimExpiresAtEastern,
                                             CancellationToken ct);

    /// <summary>
    /// Extends the lease for an already claimed page item when it is still owned by the supplied worker
    /// and lease token.
    /// </summary>
    /// <param name="runItemId">Run-item identifier.</param>
    /// <param name="claimedBy">Logical worker identifier that currently owns the lease.</param>
    /// <param name="leaseToken">Current lease ownership token.</param>
    /// <param name="heartbeatAtEastern">Eastern application timestamp of the heartbeat.</param>
    /// <param name="claimExpiresAtEastern">Eastern application timestamp of the new lease expiry.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the heartbeat succeeded; otherwise <c>false</c>.</returns>
    Task<bool> TryHeartbeatAsync(long runItemId,
                                 string claimedBy,
                                 Guid leaseToken,
                                 DateTimeOffset heartbeatAtEastern,
                                 DateTimeOffset claimExpiresAtEastern,
                                 CancellationToken ct);

    /// <summary>
    /// Marks a claimed page item as completed when it is still owned by the supplied worker
    /// and lease token.
    /// Successful completion should clear the active lease metadata.
    /// </summary>
    /// <param name="runItemId">Run-item identifier.</param>
    /// <param name="claimedBy">Logical worker identifier that currently owns the lease.</param>
    /// <param name="leaseToken">Current lease ownership token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the transition succeeded; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkCompletedAsync(long runItemId, string claimedBy, Guid leaseToken, CancellationToken ct);

    /// <summary>
    /// Marks a claimed page item as failed when it is still owned by the supplied worker
    /// and lease token.
    /// Failed transition should clear the active lease metadata and persist the failure reason.
    /// </summary>
    /// <param name="runItemId">Run-item identifier.</param>
    /// <param name="claimedBy">Logical worker identifier that currently owns the lease.</param>
    /// <param name="leaseToken">Current lease ownership token.</param>
    /// <param name="failureReason">Failure reason to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the transition succeeded; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkFailedAsync(long runItemId,
                                  string claimedBy,
                                  Guid leaseToken,
                                  string failureReason,
                                  CancellationToken ct);

    /// <summary>
    /// Gets the latest completed run item for the specified run and step.
    /// Completed terminal states include <see cref="SyncRunStatus.Completed"/> and
    /// <see cref="SyncRunStatus.CompletedWithRecoveryItems"/>.
    /// This query is intended for generic stage tracking rather than page recovery.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage or item name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest completed run item when found; otherwise <c>null</c>.</returns>
    Task<SyncRunItemDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct);

    /// <summary>
    /// Gets all failed run items for the specified run.
    /// This broad query may include generic stage markers and page work items.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Failed run-item collection.</returns>
    Task<IReadOnlyCollection<SyncRunItemDto>> GetFailedAsync(long runId, CancellationToken ct);

    /// <summary>
    /// Gets failed page items for the specified run and page-work step, ordered by page number.
    /// This query is intended for page-level recovery creation.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical page-work step name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Failed page run-item collection.</returns>
    Task<IReadOnlyCollection<SyncRunItemDto>> GetFailedPagesAsync(long runId, string step, CancellationToken ct);

    /// <summary>
    /// Returns whether any unfinished page items still exist for the supplied run and step.
    /// Unfinished items include pending items and running items with active or expired leases.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical page-work step name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when unfinished page items exist; otherwise <c>false</c>.</returns>
    Task<bool> HasUnfinishedPagesAsync(long runId, string step, CancellationToken ct);
}
