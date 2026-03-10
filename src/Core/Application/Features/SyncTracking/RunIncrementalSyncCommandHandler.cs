using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking;

/// <summary>
/// Handles incremental sync execution for a resolved scope:
/// creates/gets request, executes the run, and optionally queues recovery scope on failure.
/// </summary>
public sealed class RunIncrementalSyncCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                     ISyncRequestRunner syncRequestRunner)
    : IRequestHandler<RunIncrementalSyncCommand, long>
{
    /// <summary>
    /// Executes incremental sync for the specified command scope.
    /// </summary>
    /// <param name="request">Incremental sync command payload.</param>
    /// <param name="ct">Cancellation token from the caller/host.</param>
    /// <returns>The sync request identifier used for execution.</returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown when execution is canceled. Recovery scope is not created for cancellation paths.
    /// </exception>
    /// <exception cref="Exception">
    /// Rethrows the original execution exception after best-effort recovery scope creation for analytics categories.
    /// </exception>
    public async Task<long> Handle(RunIncrementalSyncCommand request, CancellationToken ct = default)
    {
        long requestId = await syncRequestRepository.CreateOrGetByScopeAsync(request.Category,
                                                                             SyncMode.Incremental,
                                                                             request.Interval,
                                                                             request.PageNumber,
                                                                             null,
                                                                             ct)
                                                    .ConfigureAwait(false);

        try
        {
            await syncRequestRunner.ExecuteAsync(requestId, ct)
                                   .ConfigureAwait(false);

            return requestId;
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
            if (!IsAnalyticsCategory(request.Category)) throw;

            try
            {
                _ = await syncRequestRepository.CreateOrGetByScopeAsync(request.Category,
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

    private static bool IsAnalyticsCategory(SyncCategory category)
    {
        return category is SyncCategory.UsersDetails or SyncCategory.ConversationsDetails
                                                     or SyncCategory.ConversationsAggregates;
    }
}
