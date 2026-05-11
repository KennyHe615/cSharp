using Microsoft.Extensions.Options;

using Ntt.Analytics.Scheduling;


namespace Ntt.Analytics.Workers.Recovery;

/// <summary>
/// Runs scheduled recovery intake materialization for the NTT analytics host.
/// This loop should be enabled only in the singleton planner deployment.
/// </summary>
public sealed class RecoveryIntakeMaterializationScheduledWorkerLoop(IServiceScopeFactory serviceScopeFactory,
                                                                     IOptions<CronOrIntervalOptions> options,
                                                                     ScheduledWorkerLoopRunner loopRunner)
        : IScheduledWorkerLoop
{
    private readonly IServiceScopeFactory _serviceScopeFactory =
            serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    private readonly CronOrIntervalOptions _options =
            (options ?? throw new ArgumentNullException(nameof(options))).Value;

    private readonly ScheduledWorkerLoopRunner _loopRunner =
            loopRunner ?? throw new ArgumentNullException(nameof(loopRunner));

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken ct)
    {
        if (!_options.RecoveryIntakeMaterializationEnabled) return;

        await _loopRunner.RunPeriodicAsync("Recovery intake materialization worker",
                                           TimeSpan.FromMinutes(_options.RecoveryIntakeMaterializationIntervalMinutes),
                                           RunOnceAsync,
                                           ct)
                         .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        RecoveryIntakeMaterializationWorker worker =
                scope.ServiceProvider.GetRequiredService<RecoveryIntakeMaterializationWorker>();

        await worker.RunOnceAsync(null, ct)
                    .ConfigureAwait(false);
    }

    #endregion
}
