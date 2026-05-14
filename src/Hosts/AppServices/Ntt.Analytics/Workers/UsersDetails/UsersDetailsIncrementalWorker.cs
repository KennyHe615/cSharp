using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;
using Application.Mediator;

using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Executes one NTT UsersDetails incremental sync cycle.
/// This worker populates the NTT LOB context and delegates incremental orchestration
/// to the application layer.
/// </summary>
public sealed class UsersDetailsIncrementalWorker(ISimpleMediator mediator,
                                                  ILobContextAccessor lobContextAccessor,
                                                  ICredentialProvider credentialProvider,
                                                  ILogger<UsersDetailsIncrementalWorker> logger)
{
    private static readonly LobName Lob = LobName.Ntt;

    private readonly ISimpleMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILobContextAccessor _lobContextAccessor =
            lobContextAccessor ?? throw new ArgumentNullException(nameof(lobContextAccessor));

    private readonly ICredentialProvider _credentialProvider =
            credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));

    private readonly ILogger<UsersDetailsIncrementalWorker> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes one UsersDetails incremental cycle for the current NTT host instance.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    public async Task RunOnceAsync(CancellationToken ct)
    {
        using IDisposable scope = _logger.BeginOperationScope(Lob, nameof(SyncAnalyticsCategory.UsersDetails));

        _lobContextAccessor.LobName = Lob.Value;

        await _credentialProvider.PopulateAsync(_lobContextAccessor, ct)
                                 .ConfigureAwait(false);

        _logger.LogInformation(LobLogTemplates.LobCategory + "STARTED.", Lob.Value, SyncAnalyticsCategory.UsersDetails);

        long? requestId = await _mediator.Send(new RunUsersDetailsIncrementalCycleCommand(Lob), ct)
                                         .ConfigureAwait(false);

        if (requestId.HasValue)
        {
            _logger.LogInformation(LobLogTemplates.LobCategory + "COMPLETED. RequestId = {RequestId}.",
                                   Lob.Value,
                                   SyncAnalyticsCategory.UsersDetails,
                                   requestId.Value);

            return;
        }

        _logger.LogInformation(LobLogTemplates.LobCategory + "No incremental work found.",
                               Lob.Value,
                               SyncAnalyticsCategory.UsersDetails);
    }
}
