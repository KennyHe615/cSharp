using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.Recovery;

/// <summary>
/// Handles creation of recovery requests by mapping recovery category to
/// persistence data type and creating a job tracking record.
/// </summary>
public sealed class
    CreateRecoveryRequestHandler : IRequestHandler<CreateRecoveryRequestCommand, CreateRecoveryRequestResponse>
{
    private readonly IJobTrackingRepository _jobTrackingRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRecoveryRequestHandler"/> class.
    /// </summary>
    /// <param name="jobTrackingRepository">Repository used to persist recovery job tracking records.</param>
    public CreateRecoveryRequestHandler(IJobTrackingRepository jobTrackingRepository)
    {
        _jobTrackingRepository =
            jobTrackingRepository ?? throw new ArgumentNullException(nameof(jobTrackingRepository));
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when the recovery category is unsupported.</exception>
    public async Task<CreateRecoveryRequestResponse> Handle(CreateRecoveryRequestCommand request,
                                                            CancellationToken ct = default)
    {
        SyncDataType dataType = MapCategoryToSyncDataType(request.Category);

        long createdId = await _jobTrackingRepository.CreateAsync(dataType,
                                                                  request.Interval,
                                                                  request.JobId,
                                                                  ct)
                                                     .ConfigureAwait(false);

        return new CreateRecoveryRequestResponse(true,
                                                 "Recovery request created successfully.",
                                                 new
                                                 {
                                                     Id = createdId,
                                                     Lob = request.Lob.ToString(),
                                                     Category = request.Category.ToString(),
                                                     request.Interval,
                                                     request.JobId
                                                 });
    }

    #region ========== *** Private Methods *** ==========

    private static SyncDataType MapCategoryToSyncDataType(RecoveryCategory category)
    {
        return category switch
               {
                   RecoveryCategory.UsersDetails => SyncDataType.UsersDetailsRecovery,
                   RecoveryCategory.ConversationsDetails => SyncDataType.ConversationsDetailsRecovery,
                   RecoveryCategory.ConversationsAggregates => SyncDataType.ConversationsAggregatesRecovery,
                   _ => throw new InvalidOperationException($"Unsupported recovery category '{category}'.")
               };
    }

    #endregion
}
