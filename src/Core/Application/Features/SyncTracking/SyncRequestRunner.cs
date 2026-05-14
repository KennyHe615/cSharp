using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence.SyncTracking;
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
    public Task<SyncExecutionResult> ExecuteAsync(long requestId, CancellationToken ct)
    {
        return ExecuteCoreAsync(requestId, false, ct);
    }

    /// <inheritdoc />
    public Task<SyncExecutionResult> ExecuteJoinableAsync(long requestId, CancellationToken ct)
    {
        return ExecuteCoreAsync(requestId, true, ct);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Executes one sync request using either replacement-run semantics or join-active-run semantics.
    /// </summary>
    /// <param name="requestId">Logical sync request id to execute.</param>
    /// <param name="joinActiveRun">
    /// When <c>true</c>, joins the current active run when one exists; otherwise starts a replacement run.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The sync execution result.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified sync request does not exist.
    /// </exception>
    private async Task<SyncExecutionResult> ExecuteCoreAsync(long requestId, bool joinActiveRun, CancellationToken ct)
    {
        SyncRequestDto request = await syncRequestRepository.GetByIdAsync(requestId, ct)
                                                            .ConfigureAwait(false)
                                 ?? throw new InvalidOperationException($"Sync request '{requestId}' was not found.");

        long runId = joinActiveRun
                             ? await syncRunCoordinator.StartOrJoinActiveRunAsync(requestId, ct)
                                                       .ConfigureAwait(false)
                             : await syncRunCoordinator.StartNewRunAsync(requestId, ct)
                                                       .ConfigureAwait(false);

        try
        {
            bool isCurrentRun = await syncRunCoordinator.IsCurrentRunAsync(runId, ct)
                                                        .ConfigureAwait(false);

            if (!isCurrentRun)
            {
                return new SyncExecutionResult(CompletedWithRecoveryItems: false);
            }

            SyncExecutionResult executionResult =
                    await syncExecutionDispatcher.ExecuteAsync(runId,
                                                               request.Category,
                                                               request.Mode,
                                                               request.Interval,
                                                               request.PageNumber,
                                                               request.GenesysJobId,
                                                               ct)
                                                 .ConfigureAwait(false);

            if (executionResult.Failed)
            {
                await syncRunCoordinator.MarkFailedAsync(runId,
                                                         executionResult.FailureReason ?? "Sync execution failed.",
                                                         ct)
                                        .ConfigureAwait(false);

                return executionResult;
            }

            if (executionResult.CompletedWithRecoveryItems)
            {
                await syncRunCoordinator.MarkCompletedWithRecoveryItemsAsync(runId, ct)
                                        .ConfigureAwait(false);
            }
            else
            {
                await syncRunCoordinator.MarkCompletedAsync(runId, ct)
                                        .ConfigureAwait(false);
            }

            return executionResult;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await HandleCanceledAsync(runId,
                                      joinActiveRun,
                                      "Canceled by host/user request.",
                                      CancellationToken.None)
                   .ConfigureAwait(false);

            throw;
        }
        catch (OperationCanceledException)
        {
            await HandleCanceledAsync(runId,
                                      joinActiveRun,
                                      "Canceled by orchestration signal.",
                                      CancellationToken.None)
                   .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            if (joinActiveRun) throw;

            await syncRunCoordinator.MarkFailedAsync(runId, ex.Message, CancellationToken.None)
                                    .ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Handles cancellation for one runner participant.
    /// Joinable runs are shared by multiple workers, so one participant cancellation must not
    /// finalize the shared run.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="joinActiveRun">Whether the runner joined a shared active run.</param>
    /// <param name="reason">Cancellation reason.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task HandleCanceledAsync(long runId, bool joinActiveRun, string reason, CancellationToken ct)
    {
        if (joinActiveRun) return;

        await syncRunCoordinator.MarkCanceledAsync(runId, reason, ct)
                                .ConfigureAwait(false);
    }

    #endregion
}
