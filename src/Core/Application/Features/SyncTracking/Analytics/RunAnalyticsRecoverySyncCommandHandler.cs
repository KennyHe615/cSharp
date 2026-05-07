using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.Shared;
using Application.Mediator;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Handles recovery sync execution for a resolved recovery scope.
/// Request resolution may create, reuse active, or reopen failed/canceled based on persistence rules.
/// </summary>
public sealed class RunAnalyticsRecoverySyncCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                           ISyncRequestRunner syncRequestRunner)
                : IRequestHandler<RunAnalyticsRecoverySyncCommand, long>
{
    /// <summary>
    /// Resolves a recovery request by scope, executes it, and returns the internal request id.
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

        SyncRequestResolveResult resolveResult =
                        await syncRequestRepository.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                            SyncMode.Recovery,
                                                                            request.Interval,
                                                                            request.PageNumber,
                                                                            request.GenesysJobId,
                                                                            ct)
                                                   .ConfigureAwait(false);

        await syncRequestRunner.ExecuteAsync(resolveResult.Id, ct)
                               .ConfigureAwait(false);

        return resolveResult.Id;
    }
}
