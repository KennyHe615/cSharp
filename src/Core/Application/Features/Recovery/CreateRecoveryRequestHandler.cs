using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.Recovery;

/// <summary>
/// Handles creation of recovery requests in SyncTracking (request table only).
/// </summary>
public sealed class CreateRecoveryRequestHandler(ISyncRequestRepository syncRequestRepository)
    : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
{
    private readonly ISyncRequestRepository _syncRequestRepository =
        syncRequestRepository ?? throw new ArgumentNullException(nameof(syncRequestRepository));

    public async Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request,
                                                            CancellationToken ct = default)
    {
        long requestId = await _syncRequestRepository.CreateOrGetByScopeAsync(MapCategory(request.Category),
                                                                              SyncMode.Recovery,
                                                                              request.Interval?.ToString(),
                                                                              null,
                                                                              request.GenesysJobId,
                                                                              ct)
                                                     .ConfigureAwait(false);

        return new CreateRecoveryRequestResponse(true,
                                                 "Recovery request created successfully.",
                                                 new
                                                 {
                                                     Id = requestId,
                                                     Lob = request.Lob.ToString(),
                                                     Category = request.Category.ToString(),
                                                     request.Interval,
                                                     request.GenesysJobId
                                                 });
    }

    #region ========== *** Private Methods *** ==========

    private static SyncCategory MapCategory(RecoveryCategory category)
    {
        return category switch
               {
                   RecoveryCategory.UsersDetails => SyncCategory.UsersDetails,
                   RecoveryCategory.ConversationsDetails => SyncCategory.ConversationsDetails,
                   RecoveryCategory.ConversationsAggregates => SyncCategory.ConversationsAggregates,
                   _ => throw new InvalidOperationException($"Unsupported recovery category '{category}'.")
               };
    }

    #endregion
}
