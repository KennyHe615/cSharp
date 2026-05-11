using Application.DTOs.Recovery;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for user-submitted analytics recovery intake requests.
/// Intake requests are later planned into executable sync_request rows.
/// </summary>
public interface IAnalyticsRecoveryRequestRepository
{
    /// <summary>
    /// Creates a new pending recovery intake request, or returns the existing active request for the same scope.
    /// </summary>
    /// <param name="category">Analytics recovery category token.</param>
    /// <param name="interval">Optional original UTC interval submitted by the caller.</param>
    /// <param name="genesysJobId">Optional Genesys job identifier for supported categories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Internal id, public id, and the resolution action applied by persistence.</returns>
    Task<AnalyticsRecoveryRequestResolveResult> CreateOrGetActiveAsync(string category,
                                                                       string? interval,
                                                                       string? genesysJobId,
                                                                       CancellationToken ct);

    /// <summary>
    /// Gets one recovery intake request by internal database id.
    /// </summary>
    /// <param name="id">Internal recovery intake request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The recovery intake request when found; otherwise <c>null</c>.</returns>
    Task<AnalyticsRecoveryRequestDto?> GetByIdAsync(long id, CancellationToken ct);
}
