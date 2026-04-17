using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Handles incremental sync execution for a resolved incremental scope.
/// Also performs best-effort recovery request resolution when incremental execution fails.
/// </summary>
public sealed class RunAnalyticsIncrementalSyncCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                              ISyncRequestRunner syncRequestRunner)
    : IRequestHandler<RunAnalyticsIncrementalSyncCommand, long>
{
    /// <summary>
    /// Resolves incremental request by scope, executes it, and returns internal request id.
    /// </summary>
    /// <param name="request">Incremental sync command payload.</param>
    /// <param name="ct">Cancellation token from the caller/host.</param>
    /// <returns>The internal sync request identifier used for execution.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when execution is canceled. Recovery scope is not created for cancellation paths.
    /// </exception>
    /// <exception cref="Exception">
    /// Rethrows the original execution exception after best-effort recovery scope resolution for analytics categories.
    /// </exception>
    public async Task<long> Handle(RunAnalyticsIncrementalSyncCommand request, CancellationToken ct = default)
    {
        SyncRequestResolveResult resolveResult =
            await syncRequestRepository.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                SyncMode.Incremental,
                                                                request.Interval,
                                                                request.PageNumber,
                                                                null,
                                                                ct)
                                       .ConfigureAwait(false);

        try
        {
            await syncRequestRunner.ExecuteAsync(resolveResult.Id, ct)
                                   .ConfigureAwait(false);

            return resolveResult.Id;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Host shutdown / scale-in / caller canceled.
            // Do not create recovery request.
            throw;
        }
        catch (OperationCanceledException)
        {
            // Orchestration-level cancellation signal.
            // Do not create recovery request.
            throw;
        }
        catch (Exception)
        {
            if (!AnalyticsCategoryGuards.IsAnalyticsCategory(request.Category)) throw;

            try
            {
                _ = await syncRequestRepository.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                        SyncMode.Recovery,
                                                                        request.Interval,
                                                                        request.PageNumber,
                                                                        null,
                                                                        ct)
                                               .ConfigureAwait(false);
            }
            catch
            {
                // Best-effort only. Do not hide original incremental failure.
            }

            throw;
        }
    }
}
