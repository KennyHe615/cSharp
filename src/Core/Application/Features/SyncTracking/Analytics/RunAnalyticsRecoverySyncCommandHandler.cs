using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Sync;
using Application.Enums;
using Application.Features.Shared;
using Application.Mediator;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Handles recovery sync execution for an already-claimed recovery request.
/// </summary>
public sealed class RunAnalyticsRecoverySyncCommandHandler(ISyncRequestRunner syncRequestRunner)
        : IRequestHandler<RunAnalyticsRecoverySyncCommand, long>
{
    /// <summary>
    /// Executes a claimed recovery request and returns the internal request id.
    /// </summary>
    /// <param name="request">Recovery sync command payload.</param>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns>The internal sync request identifier used for execution.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when recovery is requested for a non-analytics category,
    /// or when <c>GenesysJobId</c> is supplied for a category other than <see cref="SyncAnalyticsCategory.ConversationsDetails"/>.
    /// </exception>
    public async Task<long> Handle(RunAnalyticsRecoverySyncCommand request, CancellationToken ct = default)
    {
        if (!AnalyticsCategoryGuards.IsAnalyticsCategory(request.Category))
        {
            throw new InvalidOperationException($"Recovery mode is not supported for category '{request.Category}'.");
        }

        if (!RecoveryValidationRules.OnlyUseGenesysJobIdForConversationsDetails(request.GenesysJobId,
                                                                                    request.Category
                                                                                    == SyncAnalyticsCategory
                                                                                           .ConversationsDetails))
        {
            throw new InvalidOperationException("GenesysJobId is only supported for ConversationsDetails recovery.");
        }

        await syncRequestRunner.ExecuteAsync(request.RequestId, ct)
                               .ConfigureAwait(false);

        return request.RequestId;
    }
}
