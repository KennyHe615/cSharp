using Application.DTOs.SyncTracking;
using Application.Enums;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for logical sync request records.
/// A sync request represents one unique scope identity (category + mode + selectors).
/// </summary>
public interface ISyncRequestRepository
{
    /// <summary>
    /// Creates a new sync request for the specified scope, or returns the existing request id
    /// when the same scope already exists.
    /// </summary>
    /// <param name="category">Sync category.</param>
    /// <param name="mode">Execution mode (Incremental or Recovery).</param>
    /// <param name="interval">Optional interval selector.</param>
    /// <param name="pageNumber">Optional page selector.</param>
    /// <param name="genesysJobId">Optional external Genesys job selector.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The existing or newly created sync request id for this scope.</returns>
    Task<long> CreateOrGetByScopeAsync(string category,
                                       SyncMode mode,
                                       string? interval,
                                       int? pageNumber,
                                       string? genesysJobId,
                                       CancellationToken ct);

    /// <summary>
    /// Gets one sync request by id.
    /// </summary>
    /// <param name="id">Sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The request <see cref="SyncRequestDto"/> when found; otherwise <c>null</c>.</returns>
    Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// Updates the request pointer to the current run id.
    /// </summary>
    /// <param name="requestId">Sync request id.</param>
    /// <param name="runId">Run id to set as current.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SetCurrentRunAsync(long requestId, long runId, CancellationToken ct);
}
