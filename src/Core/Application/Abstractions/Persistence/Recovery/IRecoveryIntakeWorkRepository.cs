using Application.DTOs.Recovery;


namespace Application.Abstractions.Persistence.Recovery;

/// <summary>
/// Persistence contract for the scheduled worker that materializes recovery intake requests into executable sync work.
/// </summary>
public interface IRecoveryIntakeWorkRepository
{
    /// <summary>
    /// Atomically starts the next pending recovery intake request so it can be materialized into executable sync_request rows.
    /// </summary>
    /// <param name="category">
    /// Optional analytics recovery category token. When null, the oldest pending request across all categories is selected.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The started intake request when work is available; otherwise <c>null</c>.</returns>
    Task<AnalyticsRecoveryRequestDto?> TryStartNextPendingAsync(string? category, CancellationToken ct);

    /// <summary>
    /// Marks a running recovery intake request completed after executable sync_request rows have been created.
    /// </summary>
    /// <param name="id">Internal recovery intake request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the request was marked completed; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkCompletedAsync(long id, CancellationToken ct);

    /// <summary>
    /// Marks a running recovery intake request failed when it cannot be materialized into executable sync_request rows.
    /// </summary>
    /// <param name="id">Internal recovery intake request id.</param>
    /// <param name="failureReason">Failure reason to persist on the intake request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> when the request was marked failed; otherwise <c>false</c>.</returns>
    Task<bool> TryMarkFailedAsync(long id, string failureReason, CancellationToken ct);
}
