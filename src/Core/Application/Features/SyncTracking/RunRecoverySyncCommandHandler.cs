using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking;

/// <summary>
/// Handles explicit recovery sync execution for an existing or newly created recovery scope.
/// </summary>
public sealed class RunRecoverySyncCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                  ISyncRequestRunner syncRequestRunner)
    : IRequestHandler<RunRecoverySyncCommand, long>
{
    /// <summary>
    /// Executes a recovery sync request for the given command scope.
    /// </summary>
    /// <param name="request">Recovery sync command payload.</param>
    /// <param name="ct">Cancellation token from caller/host.</param>
    /// <returns>The sync request identifier used for execution.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when recovery is requested for a non-analytics category.
    /// </exception>
    public async Task<long> Handle(RunRecoverySyncCommand request, CancellationToken ct = default)
    {
        if (!IsAnalyticsCategory(request.Category))
        {
            throw new InvalidOperationException($"Recovery mode is not supported for category '{request.Category}'.");
        }

        long requestId = await syncRequestRepository.CreateOrGetByScopeAsync(request.Category,
                                                                             SyncMode.Recovery,
                                                                             request.Interval,
                                                                             request.PageNumber,
                                                                             request.GenesysJobId,
                                                                             ct)
                                                    .ConfigureAwait(false);

        await syncRequestRunner.ExecuteAsync(requestId, ct)
                               .ConfigureAwait(false);

        return requestId;
    }

    #region ========== *** Private Section *** ==========

    private static bool IsAnalyticsCategory(SyncCategory category)
    {
        return category is SyncCategory.UsersDetails or SyncCategory.ConversationsDetails
                                                     or SyncCategory.ConversationsAggregates;
    }

    #endregion
}
