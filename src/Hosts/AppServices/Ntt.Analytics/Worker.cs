using Microsoft.Extensions.Options;

using Ntt.Analytics.Scheduling;
using Ntt.Analytics.Workers.UsersDetails;


namespace Ntt.Analytics;

/// <summary>
/// Background scheduler for NTT analytics workers.
/// This host currently runs UsersDetails incremental and recovery workers
/// using independently configurable execution intervals.
/// </summary>
public sealed class Worker(IServiceScopeFactory serviceScopeFactory,
                           IOptions<CronOrIntervalOptions> options,
                           ILogger<Worker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory =
            serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    private readonly CronOrIntervalOptions _options =
            (options ?? throw new ArgumentNullException(nameof(options))).Value;

    private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Runs the host scheduling loops for UsersDetails incremental and recovery execution.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token propagated by the host.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer incrementalTimer =
                new PeriodicTimer(TimeSpan.FromMinutes(_options.UsersDetailsIncrementalIntervalMinutes));

        using PeriodicTimer recoveryTimer =
                new PeriodicTimer(TimeSpan.FromHours(_options.UsersDetailsRecoveryIntervalHours));

        Task incrementalLoop = RunIncrementalLoopAsync(incrementalTimer, stoppingToken);
        Task recoveryLoop = RunRecoveryLoopAsync(recoveryTimer, stoppingToken);

        await Task.WhenAll(incrementalLoop, recoveryLoop)
                  .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Runs the UsersDetails incremental scheduler loop.
    /// </summary>
    /// <param name="timer">Periodic timer for incremental execution.</param>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    private async Task RunIncrementalLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        await RunUsersDetailsIncrementalAsync(ct)
               .ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(ct)
                          .ConfigureAwait(false))
        {
            await RunUsersDetailsIncrementalAsync(ct)
                   .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Runs the UsersDetails recovery scheduler loop.
    /// </summary>
    /// <param name="timer">Periodic timer for recovery execution.</param>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    private async Task RunRecoveryLoopAsync(PeriodicTimer timer, CancellationToken ct)
    {
        await RunUsersDetailsRecoveryAsync(ct)
               .ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(ct)
                          .ConfigureAwait(false))
        {
            await RunUsersDetailsRecoveryAsync(ct)
                   .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Executes one scheduled UsersDetails incremental cycle in a fresh scope
    /// and logs unexpected host-level failures.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    private async Task RunUsersDetailsIncrementalAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            UsersDetailsIncrementalWorker worker =
                    scope.ServiceProvider.GetRequiredService<UsersDetailsIncrementalWorker>();

            await worker.RunOnceAsync(ct)
                        .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UsersDetails incremental worker failed.");
        }
    }

    /// <summary>
    /// Executes one scheduled UsersDetails recovery cycle in a fresh scope
    /// and logs unexpected host-level failures.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    private async Task RunUsersDetailsRecoveryAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = _serviceScopeFactory.CreateScope();

            UsersDetailsRecoveryWorker worker = scope.ServiceProvider.GetRequiredService<UsersDetailsRecoveryWorker>();

            await worker.RunOnceAsync(ct)
                        .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UsersDetails recovery worker failed.");
        }
    }

    #endregion
}
