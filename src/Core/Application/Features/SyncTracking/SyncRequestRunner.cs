using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;


namespace Application.Features.SyncTracking;

/// <summary>
/// Default sync request runner that coordinates run lifecycle and dispatch execution.
/// </summary>
public sealed class SyncRequestRunner(ISyncRunCoordinator syncRunCoordinator,
                                      ISyncRequestRepository syncRequestRepository,
                                      ISyncExecutionDispatcher syncExecutionDispatcher) : ISyncRequestRunner
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified sync request does not exist.
    /// </exception>
    /// <exception cref="OperationCanceledException">
    /// Thrown when execution is canceled by caller/host or orchestration signal.
    /// </exception>
    public async Task ExecuteAsync(long requestId, CancellationToken ct)
    {
        SyncRequestDto request = await syncRequestRepository.GetByIdAsync(requestId, ct)
                                                            .ConfigureAwait(false)
                                 ?? throw new InvalidOperationException($"Sync request '{requestId}' was not found.");

        long runId = await syncRunCoordinator.StartNewRunAsync(requestId, ct)
                                             .ConfigureAwait(false);

        try
        {
            bool isCurrentRun = await syncRunCoordinator.IsCurrentRunAsync(runId, ct)
                                                        .ConfigureAwait(false);

            if (!isCurrentRun) return;

            await syncExecutionDispatcher.ExecuteAsync(runId,
                                                       request.Category,
                                                       request.Mode,
                                                       request.Interval,
                                                       request.PageNumber,
                                                       request.GenesysJobId,
                                                       ct)
                                         .ConfigureAwait(false);

            await syncRunCoordinator.MarkCompletedAsync(runId, ct)
                                    .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await syncRunCoordinator.MarkCanceledAsync(runId, "Canceled by host/user request.", CancellationToken.None)
                                    .ConfigureAwait(false);

            throw;
        }
        catch (OperationCanceledException)
        {
            await syncRunCoordinator
                 .MarkCanceledAsync(runId, "Canceled by orchestration signal.", CancellationToken.None)
                 .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            await syncRunCoordinator.MarkFailedAsync(runId, ex.Message, ct)
                                    .ConfigureAwait(false);

            throw;
        }
    }
}
