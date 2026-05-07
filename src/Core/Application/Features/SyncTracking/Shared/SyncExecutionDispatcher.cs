using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;

using SharedKernel.Sync;


namespace Application.Features.SyncTracking.Shared;

/// <summary>
/// Top-level dispatcher that routes by category and mode, and tracks dispatch-stage run items.
/// Execution details are delegated to specialized orchestrators.
/// </summary>
public sealed class SyncExecutionDispatcher(ISyncRunItemRepository syncRunItemRepository,
                                            IReferencesSyncOrchestrator referencesSyncOrchestrator)
                : ISyncExecutionDispatcher
{
    /// <inheritdoc />
    public async Task<SyncExecutionResult> ExecuteAsync(long runId,
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

        await syncRunItemRepository.UpsertAsync(runId,
                                                SyncRunItemSteps.Dispatch,
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

            await syncRunItemRepository.UpsertAsync(runId,
                                                    SyncRunItemSteps.Dispatch,
                                                    scopeKey,
                                                    SyncRunStatus.Completed,
                                                    null,
                                                    ct)
                                       .ConfigureAwait(false);

            return new SyncExecutionResult(CompletedWithRecoveryItems: false);
        }
        catch (OperationCanceledException ex)
        {
            await syncRunItemRepository.UpsertAsync(runId,
                                                    SyncRunItemSteps.Dispatch,
                                                    scopeKey,
                                                    SyncRunStatus.Canceled,
                                                    ex.Message,
                                                    CancellationToken.None)
                                       .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            await syncRunItemRepository.UpsertAsync(runId,
                                                    SyncRunItemSteps.Dispatch,
                                                    scopeKey,
                                                    SyncRunStatus.Failed,
                                                    ex.Message,
                                                    CancellationToken.None)
                                       .ConfigureAwait(false);

            throw;
        }
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Dispatches one run to the correct category-specific execution pipeline.
    /// </summary>
    private Task DispatchByScopeAsync(long runId, string category, SyncMode mode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new NotSupportedException("Category is required for dispatch.");
        }

        if (Enum.TryParse(category, true, out SyncReferenceCategory referenceCategory))
        {
            return mode != SyncMode.Full
                                   ? throw new NotSupportedException("References full-sync accepts Full mode only.")
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
