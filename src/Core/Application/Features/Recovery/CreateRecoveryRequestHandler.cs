using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.Recovery;

/// <summary>
/// Handles recovery request resolution in SyncTracking (create/reuse/reopen by scope rules).
/// </summary>
public sealed class CreateRecoveryRequestHandler(ISyncRequestRepository syncRequestRepository)
                : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
{
    private readonly ISyncRequestRepository _syncRequestRepository =
                    syncRequestRepository ?? throw new ArgumentNullException(nameof(syncRequestRepository));

    public async Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request,
                                                            CancellationToken ct = default)
    {
        string category = MapCategory(request.Category)
                       .ToString();

        SyncRequestResolveResult resolveResult =
                        await _syncRequestRepository.CreateOrGetByScopeAsync(category,
                                                                             SyncMode.Recovery,
                                                                             request.Interval?.ToString(),
                                                                             null,
                                                                             request.GenesysJobId,
                                                                             ct)
                                                    .ConfigureAwait(false);

        CreateRecoveryRequestResponseData data = new CreateRecoveryRequestResponseData(resolveResult.PublicId,
            resolveResult.RequestAction.ToString(),
            request.Lob.ToString(),
            request.Category.ToString(),
            request.Interval,
            request.GenesysJobId);

        return new CreateRecoveryRequestResponse(true, "Recovery request accepted.", data);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Maps API-level recovery categories to analytics sync categories.
    /// Kept explicit to preserve contract boundary between HTTP request model and sync domain enum.
    /// </summary>
    private static SyncAnalyticsCategory MapCategory(RecoveryCategory category)
    {
        return category switch
               {
                   RecoveryCategory.UsersDetails => SyncAnalyticsCategory.UsersDetails,
                   RecoveryCategory.ConversationsDetails => SyncAnalyticsCategory.ConversationsDetails,
                   RecoveryCategory.ConversationsAggregates => SyncAnalyticsCategory.ConversationsAggregates,
                   _ => throw new InvalidOperationException($"Unsupported recovery category '{category}'.")
               };
    }

    #endregion
}
