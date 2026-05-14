using Ntt.Analytics.Scheduling;


namespace Ntt.Analytics;

/// <summary>
/// Background scheduler host for NTT analytics worker loops.
/// Category and mode scheduling is delegated to registered <see cref="IScheduledWorkerLoop"/> implementations.
/// </summary>
public sealed class Worker(IServiceScopeFactory serviceScopeFactory,
                           ILogger<Worker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory =
            serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    private readonly ILogger<Worker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Runs all registered NTT analytics scheduled worker loops.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token propagated by the host.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();

        IScheduledWorkerLoop[] scheduledWorkerLoops = scope.ServiceProvider.GetServices<IScheduledWorkerLoop>()
                                                           .ToArray();

        if (scheduledWorkerLoops.Length == 0)
        {
            _logger.LogWarning("No scheduled worker loops are registered for NTT analytics host.");

            return;
        }

        Task[] loops = scheduledWorkerLoops.Select(loop => loop.RunAsync(stoppingToken))
                                           .ToArray();

        await Task.WhenAll(loops)
                  .ConfigureAwait(false);
    }
}
