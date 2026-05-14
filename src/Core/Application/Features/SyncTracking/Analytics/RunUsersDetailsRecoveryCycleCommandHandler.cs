using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Handles one UsersDetails recovery cycle by atomically starting one eligible recovery request
/// and dispatching recovery execution for that request.
/// </summary>
public sealed class RunUsersDetailsRecoveryCycleCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                               ISimpleMediator mediator)
        : IRequestHandler<RunUsersDetailsRecoveryCycleCommand, long?>
{
    /// <summary>
    /// Executes one UsersDetails recovery cycle and returns the claimed request id when work was found.
    /// </summary>
    /// <param name="request">Recovery cycle command marker.</param>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns>The claimed recovery request id when work ran; otherwise <c>null</c>.</returns>
    public async Task<long?> Handle(RunUsersDetailsRecoveryCycleCommand request, CancellationToken ct = default)
    {
        long? lastRequestId = null;

        while (true)
        {
            SyncRequestDto? recoveryRequest =
                    await syncRequestRepository
                         .TryStartNextRecoveryRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct)
                         .ConfigureAwait(false);

            if (recoveryRequest is null) return lastRequestId;

            lastRequestId = await mediator.Send(new RunAnalyticsRecoverySyncCommand(recoveryRequest.Id,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    recoveryRequest.Interval,
                                                    recoveryRequest.PageNumber,
                                                    null),
                                                ct)
                                          .ConfigureAwait(false);
        }
    }
}
