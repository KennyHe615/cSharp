using Application.DTOs.SyncTracking;
using Application.Enums;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for logical sync request records.
/// Incremental mode keeps one logical request per scope; recovery mode resolves by active/reopen/create rules.
/// </summary>
public interface ISyncRequestRepository
{
    /// <summary>
    /// Resolves a sync request for the specified scope.
    /// Incremental mode reuses the existing scope row when present.
    /// Recovery mode returns an active row, reopens latest failed/canceled row, or creates a new row.
    /// </summary>
    /// <param name="category">Sync category token.</param>
    /// <param name="mode">Execution mode (Incremental or Recovery).</param>
    /// <param name="interval">Optional interval selector.</param>
    /// <param name="pageNumber">Optional page selector.</param>
    /// <param name="genesysJobId">Optional external provider job selector.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Internal id, public id, and the resolution action applied by persistence.
    /// </returns>
    Task<SyncRequestResolveResult> CreateOrGetByScopeAsync(string category,
                                                           SyncMode mode,
                                                           string? interval,
                                                           int? pageNumber,
                                                           string? genesysJobId,
                                                           CancellationToken ct);

    /// <summary>
    /// Atomically starts the next eligible recovery request for one analytics category.
    /// Eligibility is the same as <see cref="GetEligibleRecoveryRequestsAsync"/>, but this method claims one row
    /// by moving it to <see cref="SyncRequestStatus.Running"/> before returning it.
    /// </summary>
    /// <param name="category">Recovery target category token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The claimed recovery request, or <c>null</c> when no eligible request exists.</returns>
    Task<SyncRequestDto?> TryStartNextRecoveryRequestAsync(string category, CancellationToken ct);

    /// <summary>
    /// Gets one sync request by internal database id.
    /// </summary>
    /// <param name="id">Internal sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The request <see cref="SyncRequestDto"/> when found; otherwise <c>null</c>.</returns>
    Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// Gets the next pending or running incremental request that another worker can join.
    /// This does not claim ownership of the request; page-level ownership is handled by
    /// <c>sync_run_item</c> leases inside the executable run.
    /// </summary>
    /// <param name="category">Sync category name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The next joinable incremental request when one exists; otherwise <c>null</c>.</returns>
    Task<SyncRequestDto?> GetNextJoinableIncrementalRequestAsync(string category, CancellationToken ct);

    /// <summary>
    /// Lists eligible recovery requests for one analytics category.
    /// This query is for diagnostics and non-scaled inspection. Scaled workers should use
    /// <see cref="TryStartNextRecoveryRequestAsync"/> to claim one request atomically.
    /// </summary>
    /// <param name="category">Recovery target category token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Eligible recovery request rows for the specified category, ordered by oldest actionable work first.</returns>
    Task<IReadOnlyCollection<SyncRequestDto>> GetEligibleRecoveryRequestsAsync(string category, CancellationToken ct);
}
