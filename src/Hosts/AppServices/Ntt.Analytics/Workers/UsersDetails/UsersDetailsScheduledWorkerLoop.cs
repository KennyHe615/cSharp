using Microsoft.Extensions.Options;

using Ntt.Analytics.Scheduling;


namespace Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Runs scheduled UsersDetails incremental and recovery loops for the NTT analytics host.
/// </summary>
public sealed class UsersDetailsScheduledWorkerLoop(IServiceScopeFactory serviceScopeFactory,
                                                    IOptions<CronOrIntervalOptions> options,
                                                    ScheduledWorkerLoopRunner loopRunner) : IScheduledWorkerLoop
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
        Task incrementalLoop =
                _loopRunner.RunPeriodicAsync("UsersDetails incremental worker",
                                             TimeSpan.FromMinutes(_options.UsersDetailsIncrementalIntervalMinutes),
                                             RunIncrementalOnceAsync,
                                             ct);

        Task recoveryLoop =
                _loopRunner.RunPeriodicAsync("UsersDetails recovery worker",
                                             TimeSpan.FromHours(_options.UsersDetailsRecoveryIntervalHours),
                                             RunRecoveryOnceAsync,
                                             ct);

        await Task.WhenAll(incrementalLoop, recoveryLoop)
                  .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private async Task RunIncrementalOnceAsync(CancellationToken ct)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        UsersDetailsIncrementalWorker worker =
                scope.ServiceProvider.GetRequiredService<UsersDetailsIncrementalWorker>();

        await worker.RunOnceAsync(ct)
                    .ConfigureAwait(false);
    }

    private async Task RunRecoveryOnceAsync(CancellationToken ct)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        UsersDetailsRecoveryWorker worker = scope.ServiceProvider.GetRequiredService<UsersDetailsRecoveryWorker>();

        await worker.RunOnceAsync(ct)
                    .ConfigureAwait(false);
    }

    #endregion
}
