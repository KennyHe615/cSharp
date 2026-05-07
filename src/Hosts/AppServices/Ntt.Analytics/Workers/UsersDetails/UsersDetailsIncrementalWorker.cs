using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;
using Application.Mediator;

using SharedKernel.Lobs;
using SharedKernel.Logging;
using SharedKernel.Time;


namespace Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Executes one NTT UsersDetails incremental sync cycle.
/// This worker reserves the next incremental window, populates the NTT LOB context,
/// and dispatches the corresponding application command when a new window is available.
/// </summary>
public sealed class UsersDetailsIncrementalWorker(ISimpleMediator mediator,
                                                  ILobContextAccessor lobContextAccessor,
                                                  ICredentialProvider credentialProvider,
                                                  IIncrementalSyncWindowRepository incrementalSyncWindowRepository,
                                                  IDateTimeProvider dateTimeProvider,
                                                  ILogger<UsersDetailsIncrementalWorker> logger)
{
    private static readonly LobName Lob = LobName.Ntt;

    private readonly ISimpleMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILobContextAccessor _lobContextAccessor =
            lobContextAccessor ?? throw new ArgumentNullException(nameof(lobContextAccessor));

    private readonly ICredentialProvider _credentialProvider =
            credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));

    private readonly IIncrementalSyncWindowRepository _incrementalSyncWindowRepository =
            incrementalSyncWindowRepository ?? throw new ArgumentNullException(nameof(incrementalSyncWindowRepository));

    private readonly IDateTimeProvider _dateTimeProvider =
            dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

    private readonly ILogger<UsersDetailsIncrementalWorker> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes one UsersDetails incremental cycle for the current NTT host instance.
    /// If no forward-progress window can be reserved, the method returns without dispatching a sync request.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        IncrementalSyncWindowReservation reservation =
                await _incrementalSyncWindowRepository.ReserveNextWindowAsync(Lob,
                                                                              SyncAnalyticsCategory.UsersDetails,
                                                                              _dateTimeProvider.EstNowOffset,
                                                                              ct)
                                                      .ConfigureAwait(false);

        if (!reservation.Reserved || string.IsNullOrWhiteSpace(reservation.IntervalUtc))
        {
            _logger.LogInformation(LobLogTemplates.LobCategory + "No new incremental interval reserved.",
                                   Lob.Value,
                                   SyncAnalyticsCategory.UsersDetails);

            return;
        }

        using IDisposable scope = _logger.BeginOperationScope(Lob, SyncAnalyticsCategory.UsersDetails.ToString());

        _logger.LogInformation(LobLogTemplates.LobCategory + "STARTED. Interval = {Interval}.",
                               Lob.Value,
                               SyncAnalyticsCategory.UsersDetails,
                               reservation.IntervalUtc);

        _lobContextAccessor.LobName = Lob.Value;

        await _credentialProvider.PopulateAsync(_lobContextAccessor, ct)
                                 .ConfigureAwait(false);

        _ = await _mediator.Send(new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                                        reservation.IntervalUtc,
                                                                        null),
                                 ct)
                           .ConfigureAwait(false);

        _logger.LogInformation(LobLogTemplates.LobCategory + "COMPLETED. Interval = {Interval}.",
                               Lob.Value,
                               SyncAnalyticsCategory.UsersDetails,
                               reservation.IntervalUtc);
    }
}
