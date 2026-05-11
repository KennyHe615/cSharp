namespace Ntt.Analytics.Scheduling;

/// <summary>
/// Runs scoped worker actions on a fixed periodic interval with host-level error handling.
/// </summary>
public sealed class ScheduledWorkerLoopRunner(ILogger<ScheduledWorkerLoopRunner> logger)
{
    private readonly ILogger<ScheduledWorkerLoopRunner> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Runs the supplied worker action immediately, then repeats it on the supplied interval until cancellation.
    /// </summary>
    /// <param name="workerName">Worker name used for host-level error logging.</param>
    /// <param name="interval">Delay between scheduled worker attempts.</param>
    /// <param name="runOnceAsync">Worker action to execute once per tick.</param>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    public async Task RunPeriodicAsync(string workerName,
                                       TimeSpan interval,
                                       Func<CancellationToken, Task> runOnceAsync,
                                       CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workerName);
        ArgumentNullException.ThrowIfNull(runOnceAsync);

        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval),
                                                  interval,
                                                  "Scheduled worker interval must be greater than zero.");
        }

        using PeriodicTimer timer = new PeriodicTimer(interval);

        await RunSafelyAsync(workerName, runOnceAsync, ct)
               .ConfigureAwait(false);

        while (await timer.WaitForNextTickAsync(ct)
                          .ConfigureAwait(false))
        {
            await RunSafelyAsync(workerName, runOnceAsync, ct)
                   .ConfigureAwait(false);
        }
    }

    #region ========== *** Private Section *** ==========

    private async Task RunSafelyAsync(string workerName,
                                      Func<CancellationToken, Task> runOnceAsync,
                                      CancellationToken ct)
    {
        try
        {
            await runOnceAsync(ct)
                   .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{WorkerName} failed.", workerName);
        }
    }

    #endregion
}
