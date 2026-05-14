using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.Recovery;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.Recovery;
using Application.Enums;
using Application.Features.Shared;
using Application.Mediator;


namespace Application.Features.Recovery;

/// <summary>
/// Handles recovery intake request resolution before the request is planned into executable sync work.
/// </summary>
public sealed class CreateRecoveryRequestHandler(IAnalyticsRecoveryRequestRepository recoveryRequestRepository)
        : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
{
    private readonly IAnalyticsRecoveryRequestRepository _recoveryRequestRepository =
            recoveryRequestRepository ?? throw new ArgumentNullException(nameof(recoveryRequestRepository));

    /// <summary>
    /// Resolves a recovery intake request by scope and returns the accepted recovery response payload.
    /// </summary>
    /// <param name="request">Recovery request command payload.</param>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns>The accepted recovery request response.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>GenesysJobId</c> is supplied for a category other than <see cref="RecoveryCategory.ConversationsDetails"/>.
    /// </exception>
    public async Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request,
                                                            CancellationToken ct = default)
    {
        if (!RecoveryValidationRules.OnlyUseGenesysJobIdForConversationsDetails(request.GenesysJobId,
                                                                                    request.Category
                                                                                    == RecoveryCategory
                                                                                           .ConversationsDetails))
        {
            throw new InvalidOperationException("GenesysJobId is only supported for ConversationsDetails recovery.");
        }

        string category = MapCategory(request.Category)
               .ToString();

        AnalyticsRecoveryRequestResolveResult resolveResult =
                await _recoveryRequestRepository.CreateOrGetActiveAsync(category,
                                                                        request.Interval?.ToString(),
                                                                        request.GenesysJobId,
                                                                        ct)
                                                .ConfigureAwait(false);

        CreateRecoveryRequestResponseData data =
                new CreateRecoveryRequestResponseData(resolveResult.PublicId,
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
