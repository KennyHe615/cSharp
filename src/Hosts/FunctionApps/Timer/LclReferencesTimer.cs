using Microsoft.Azure.Functions.Worker;

using SharedKernel.Lobs;


namespace FunctionApps.Timer;

/// <summary>
/// Timer trigger entry point for LCL references full-sync execution.
/// Delegates orchestration to <see cref="IReferencesTimerRunner"/>.
/// </summary>
public sealed class LclReferencesTimer(IReferencesTimerRunner referencesTimerRunner)
{
    /// <summary>
    /// Executes one LCL references full-sync cycle using the configured timer schedule.
    /// </summary>
    /// <param name="timer">Timer trigger metadata for this invocation.</param>
    /// <param name="ct">Cancellation token propagated by the function host.</param>
    [Function("LclReferencesFullSyncTimer")]
    public Task RunAsync([TimerTrigger("%LclReferencesFullSyncSchedule%")] TimerInfo timer, CancellationToken ct)
    {
        return referencesTimerRunner.RunAsync(LobName.Lcl, timer, ct);
    }
}
