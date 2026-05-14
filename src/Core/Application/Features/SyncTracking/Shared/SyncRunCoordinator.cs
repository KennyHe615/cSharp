using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence;


namespace Application.Features.SyncTracking.Shared;

/// <summary>
/// Default coordinator that delegates run lifecycle operations to persistence.
/// </summary>
public sealed class SyncRunCoordinator(ISyncRunRepository syncRunRepository) : ISyncRunCoordinator
{
    /// <inheritdoc />
    public Task<long> StartNewRunAsync(long requestId, CancellationToken ct)
    {
        return syncRunRepository.StartNewRunAsync(requestId, ct);
    }

    /// <inheritdoc />
    public Task<long> StartOrJoinActiveRunAsync(long requestId, CancellationToken ct)
    {
        return syncRunRepository.StartOrJoinActiveRunAsync(requestId, ct);
    }

    /// <inheritdoc />
    public Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct)
    {
        return syncRunRepository.IsCurrentRunAsync(runId, ct);
    }

    /// <inheritdoc />
    public Task MarkCompletedAsync(long runId, CancellationToken ct)
    {
        return syncRunRepository.MarkCompletedAsync(runId, ct);
    }

    /// <inheritdoc />
    public Task MarkCompletedWithRecoveryItemsAsync(long runId, CancellationToken ct)
    {
        return syncRunRepository.MarkCompletedWithRecoveryItemsAsync(runId, ct);
    }

    /// <inheritdoc />
    public Task MarkFailedAsync(long runId, string reason, CancellationToken ct)
    {
        return syncRunRepository.MarkFailedAsync(runId, reason, ct);
    }

    /// <inheritdoc />
    public Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct)
    {
        return syncRunRepository.MarkSupersededAsync(runId, supersededByRunId, ct);
    }

    /// <inheritdoc />
    public Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct)
    {
        return syncRunRepository.MarkCanceledAsync(runId, reason, ct);
    }
}
