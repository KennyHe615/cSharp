using Application.Enums;

using SharedKernel.Lobs;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Persists and atomically advances incremental scheduling windows independently of sync request execution state.
/// This is used to reserve the next interval before dispatch so overlapping workers can run safely
/// without falsifying <c>sync_request</c> lifecycle states.
/// </summary>
public interface IIncrementalSyncWindowRepository
{
    /// <summary>
    /// Atomically reserves the next incremental window for the specified LOB and analytics category.
    /// The next window start comes from the last reserved end timestamp; when no prior reservation exists,
    /// the start falls back to the beginning of the current Eastern day.
    /// </summary>
    /// <param name="lob">Target LOB.</param>
    /// <param name="category">Incremental analytics category.</param>
    /// <param name="intervalEndEastern">
    /// Current worker cutoff time in Eastern time. The implementation should normalize this to the intended boundary.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A reservation result describing whether a new window was reserved and, when reserved,
    /// the resulting UTC interval string that should be persisted into <c>sync_request.interval</c>.
    /// </returns>
    Task<IncrementalSyncWindowReservation> ReserveNextWindowAsync(LobName lob,
                                                                  SyncAnalyticsCategory category,
                                                                  DateTimeOffset intervalEndEastern,
                                                                  CancellationToken ct);
}

/// <summary>
/// Result of one incremental window reservation attempt.
/// </summary>
/// <param name="Reserved">
/// <c>true</c> when a new window was successfully reserved; otherwise <c>false</c>.
/// </param>
/// <param name="IntervalUtc">
/// Reserved UTC interval string to persist into <c>sync_request.interval</c> when <paramref name="Reserved"/> is <c>true</c>.
/// </param>
/// <param name="StartUtc">Reserved interval start in UTC.</param>
/// <param name="EndUtc">Reserved interval end in UTC.</param>
public sealed record IncrementalSyncWindowReservation(bool Reserved,
                                                      string? IntervalUtc,
                                                      DateTimeOffset? StartUtc,
                                                      DateTimeOffset? EndUtc);
