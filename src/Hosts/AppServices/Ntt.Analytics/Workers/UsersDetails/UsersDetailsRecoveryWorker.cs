using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;
using Application.Mediator;

using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Executes one NTT UsersDetails proactive recovery cycle.
/// This worker loads all eligible UsersDetails recovery requests for the current LOB
/// and dispatches one recovery command per request scope.
/// </summary>
public sealed class UsersDetailsRecoveryWorker(ISimpleMediator mediator,
                                               ILobContextAccessor lobContextAccessor,
                                               ICredentialProvider credentialProvider,
                                               ISyncRequestRepository syncRequestRepository,
                                               ILogger<UsersDetailsRecoveryWorker> logger)
{
    private static readonly LobName Lob = LobName.Ntt;

    private readonly ISimpleMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILobContextAccessor _lobContextAccessor =
            lobContextAccessor ?? throw new ArgumentNullException(nameof(lobContextAccessor));

    private readonly ICredentialProvider _credentialProvider =
            credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));

    private readonly ISyncRequestRepository _syncRequestRepository =
            syncRequestRepository ?? throw new ArgumentNullException(nameof(syncRequestRepository));

    private readonly ILogger<UsersDetailsRecoveryWorker> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes one UsersDetails proactive recovery cycle for the current NTT host instance.
    /// When no eligible recovery requests exist, the method returns without dispatching work.
    /// Individual request failures are logged and do not stop the remaining recovery batch.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        IReadOnlyCollection<SyncRequestDto> recoveryRequests =
                await _syncRequestRepository
                     .GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct)
                     .ConfigureAwait(false);

        if (recoveryRequests.Count == 0)
        {
            _logger.LogInformation(LobLogTemplates.LobCategory + "No eligible recovery requests found.",
                                   Lob.Value,
                                   SyncAnalyticsCategory.UsersDetails);

            return;
        }

        using IDisposable scope = _logger.BeginOperationScope(Lob, nameof(SyncAnalyticsCategory.UsersDetails));

        _logger.LogInformation(LobLogTemplates.LobCategory
                               + "STARTED. Recovery request count = {RecoveryRequestCount}.",
                               Lob.Value,
                               SyncAnalyticsCategory.UsersDetails,
                               recoveryRequests.Count);

        _lobContextAccessor.LobName = Lob.Value;

        await _credentialProvider.PopulateAsync(_lobContextAccessor, ct)
                                 .ConfigureAwait(false);

        foreach (SyncRequestDto recoveryRequest in recoveryRequests)
        {
            try
            {
                _ = await _mediator.Send(new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                                             recoveryRequest.Interval,
                                                                             recoveryRequest.PageNumber,
                                                                             null),
                                         ct)
                                   .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                                 LobLogTemplates.LobCategory
                                 + "Recovery request failed. RequestId = {RequestId}, Interval = {Interval}, PageNumber = {PageNumber}.",
                                 Lob.Value,
                                 SyncAnalyticsCategory.UsersDetails,
                                 recoveryRequest.PublicId,
                                 recoveryRequest.Interval,
                                 recoveryRequest.PageNumber);
            }
        }

        _logger.LogInformation(LobLogTemplates.LobCategory
                               + "COMPLETED. Recovery request count = {RecoveryRequestCount}.",
                               Lob.Value,
                               SyncAnalyticsCategory.UsersDetails,
                               recoveryRequests.Count);
    }
}
