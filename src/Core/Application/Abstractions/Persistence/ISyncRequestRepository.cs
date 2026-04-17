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
    /// Gets one sync request by internal database id.
    /// </summary>
    /// <param name="id">Internal sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The request <see cref="SyncRequestDto"/> when found; otherwise <c>null</c>.</returns>
    Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct);
}
