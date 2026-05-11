using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Enums;
using Application.Features.Recovery;
using Application.Mediator;

using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace Ntt.Analytics.Workers.Recovery;

/// <summary>
/// Executes one recovery intake materialization cycle for the NTT analytics host.
/// This worker should be deployed as a singleton planner app and converts intake rows into executable sync_request rows.
/// </summary>
public sealed class RecoveryIntakeMaterializationWorker(ISimpleMediator mediator,
                                                        ILobContextAccessor lobContextAccessor,
                                                        ICredentialProvider credentialProvider,
                                                        ILogger<RecoveryIntakeMaterializationWorker> logger)
{
    private static readonly LobName Lob = LobName.Ntt;

    private readonly ISimpleMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILobContextAccessor _lobContextAccessor =
            lobContextAccessor ?? throw new ArgumentNullException(nameof(lobContextAccessor));

    private readonly ICredentialProvider _credentialProvider =
            credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));

    private readonly ILogger<RecoveryIntakeMaterializationWorker> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes one materialization cycle for the supplied analytics category.
    /// When no pending intake request exists, the method returns without creating executable sync work.
    /// </summary>
    /// <param name="category">Optional analytics category filter. When null, the oldest pending request is processed.</param>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    public async Task RunOnceAsync(SyncAnalyticsCategory? category, CancellationToken ct)
    {
        using IDisposable scope = _logger.BeginOperationScope(Lob, category?.ToString() ?? "AnalyticsRecoveryIntake");

        _lobContextAccessor.LobName = Lob.Value;

        await _credentialProvider.PopulateAsync(_lobContextAccessor, ct)
                                 .ConfigureAwait(false);

        bool processed = await _mediator.Send(new MaterializeRecoveryIntakeCommand(category), ct)
                                        .ConfigureAwait(false);

        if (!processed)
        {
            _logger.LogInformation(LobLogTemplates.LobCategory + "No pending recovery intake request found.",
                                   Lob.Value,
                                   category?.ToString() ?? "AnalyticsRecoveryIntake");

            return;
        }

        _logger.LogInformation(LobLogTemplates.LobCategory + "Recovery intake request materialized.",
                               Lob.Value,
                               category?.ToString() ?? "AnalyticsRecoveryIntake");
    }
}
