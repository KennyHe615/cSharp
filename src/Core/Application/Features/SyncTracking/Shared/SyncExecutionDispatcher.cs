using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;

using SharedKernel.Sync;


namespace Application.Features.SyncTracking.Shared;

/// <summary>
/// Top-level dispatcher that routes by category and mode, and tracks dispatch-stage run items.
/// Execution details are delegated to specialized orchestrators and analytics executors.
/// </summary>
public sealed class SyncExecutionDispatcher(ISyncRunItemRepository syncRunItemRepository,
                                            IReferencesSyncOrchestrator referencesSyncOrchestrator,
                                            IEnumerable<IAnalyticsSyncExecutor> analyticsExecutors)
        : ISyncExecutionDispatcher
{
    private readonly Dictionary<SyncAnalyticsCategory, IAnalyticsSyncExecutor> _analyticsExecutors =
            (analyticsExecutors ?? throw new ArgumentNullException(nameof(analyticsExecutors)))
           .ToDictionary(x => x.Category);

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
            SyncExecutionResult executionResult = await DispatchByScopeAsync(runId,
                                                                             category,
                                                                             mode,
                                                                             interval,
                                                                             pageNumber,
                                                                             genesysJobId,
                                                                             ct)
                                                         .ConfigureAwait(false);

            SyncRunStatus dispatchStatus = executionResult.Failed ? SyncRunStatus.Failed : SyncRunStatus.Completed;

            await syncRunItemRepository.UpsertAsync(runId,
                                                    SyncRunItemSteps.Dispatch,
                                                    scopeKey,
                                                    dispatchStatus,
                                                    executionResult.FailureReason,
                                                    ct)
                                       .ConfigureAwait(false);

            return executionResult;
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
    private async Task<SyncExecutionResult> DispatchByScopeAsync(long runId,
                                                                 string category,
                                                                 SyncMode mode,
                                                                 string? interval,
                                                                 int? pageNumber,
                                                                 string? genesysJobId,
                                                                 CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new NotSupportedException("Category is required for dispatch.");
        }

        if (Enum.TryParse(category, true, out SyncReferenceCategory referenceCategory))
        {
            if (mode != SyncMode.Full)
            {
                throw new NotSupportedException("References full-sync accepts Full mode only.");
            }

            await referencesSyncOrchestrator.ExecuteAsync(runId, referenceCategory, ct)
                                            .ConfigureAwait(false);

            return new SyncExecutionResult(CompletedWithRecoveryItems: false);
        }

        if (Enum.TryParse(category, true, out SyncAnalyticsCategory analyticsCategory))
        {
            if (mode != SyncMode.Incremental && mode != SyncMode.Recovery)
            {
                throw new NotSupportedException("Analytics sync accepts Incremental or Recovery mode only.");
            }

            if (!_analyticsExecutors.TryGetValue(analyticsCategory, out IAnalyticsSyncExecutor? executor))
            {
                throw new
                        NotSupportedException($"No analytics executor is registered for category '{analyticsCategory}'.");
            }

            return await executor.ExecuteAsync(runId,
                                               mode,
                                               interval,
                                               pageNumber,
                                               genesysJobId,
                                               ct)
                                 .ConfigureAwait(false);
        }

        throw new NotSupportedException($"Unsupported sync execution route: Category={category}, Mode={mode}.");
    }

    #endregion
}
