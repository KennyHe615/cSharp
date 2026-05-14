namespace Application.Abstractions.Persistence.SyncTracking;

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
