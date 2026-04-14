using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;

using SharedKernel.Sync;


namespace Application.Features.SyncTracking.Shared;

/// <summary>
/// Top-level dispatcher that routes by category/mode and tracks dispatch-stage status.
/// Execution details are delegated to specialized orchestrators.
/// </summary>
public sealed class SyncExecutionDispatcher(ISyncCheckpointRepository syncCheckpointRepository,
                                            IReferencesSyncOrchestrator referencesSyncOrchestrator)
    : ISyncExecutionDispatcher
{
    /// <inheritdoc />
    public async Task ExecuteAsync(long runId,
                                   string category,
                                   SyncMode mode,
                                   string? interval,
                                   int? pageNumber,
                                   string? genesysJobId,
                                   CancellationToken ct)
    {
        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       mode.ToString(),
                                                       interval,
                                                       pageNumber,
                                                       genesysJobId);

        await syncCheckpointRepository.UpsertAsync(runId,
                                                   SyncCheckpointSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct)
                                      .ConfigureAwait(false);

        try
        {
            await DispatchByScopeAsync(runId,
                                       category,
                                       mode,
                                       ct)
               .ConfigureAwait(false);

            await syncCheckpointRepository.UpsertAsync(runId,
                                                       SyncCheckpointSteps.Dispatch,
                                                       scopeKey,
                                                       SyncRunStatus.Completed,
                                                       null,
                                                       ct)
                                          .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            await syncCheckpointRepository.UpsertAsync(runId,
                                                       SyncCheckpointSteps.Dispatch,
                                                       scopeKey,
                                                       SyncRunStatus.Canceled,
                                                       ex.Message,
                                                       CancellationToken.None)
                                          .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            await syncCheckpointRepository.UpsertAsync(runId,
                                                       SyncCheckpointSteps.Dispatch,
                                                       scopeKey,
                                                       SyncRunStatus.Failed,
                                                       ex.Message,
                                                       CancellationToken.None)
                                          .ConfigureAwait(false);

            throw;
        }
    }

    #region ========== *** Private Section *** ==========

    private Task DispatchByScopeAsync(long runId, string category, SyncMode mode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new NotSupportedException("Category is required for dispatch.");
        }

        if (Enum.TryParse(category, true, out SyncReferenceCategory referenceCategory))
        {
            return mode != SyncMode.Incremental
                ? throw new NotSupportedException("References full-sync accepts Incremental mode only.")
                : referencesSyncOrchestrator.ExecuteAsync(runId, referenceCategory, ct);
        }

        if (Enum.TryParse(category, true, out SyncAnalyticsCategory _))
        {
            throw new
                NotSupportedException("Analytics dispatch is temporarily disabled during References-first implementation.");
        }

        throw new NotSupportedException($"Unsupported sync execution route: Category={category}, Mode={mode}.");
    }

    #endregion
}
