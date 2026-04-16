using Microsoft.Azure.Functions.Worker;

using SharedKernel.Lobs;


namespace FunctionApps.Timer;

/// <summary>
/// Timer trigger entry point for NTT references full-sync execution.
/// Delegates orchestration to <see cref="IReferencesTimerRunner"/>.
/// </summary>
public sealed class NttReferencesTimer(IReferencesTimerRunner referencesTimerRunner)
{
    /// <summary>
    /// Executes one NTT references full-sync cycle using the configured timer schedule.
    /// </summary>
    /// <param name="timer">Timer trigger metadata for this invocation.</param>
    /// <param name="ct">Cancellation token propagated by the function host.</param>
    [Function("NttReferencesFullSyncTimer")]
    public Task RunAsync([TimerTrigger("%NttReferencesFullSyncSchedule%")] TimerInfo timer, CancellationToken ct)
    {
        return referencesTimerRunner.RunAsync(LobName.Ntt, timer, ct);
    }
}
