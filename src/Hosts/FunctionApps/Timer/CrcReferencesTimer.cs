using Microsoft.Azure.Functions.Worker;

using SharedKernel.Lobs;


namespace FunctionApps.Timer;

/// <summary>
/// Timer trigger entry point for CRC references full-sync execution.
/// Delegates orchestration to <see cref="IReferencesTimerRunner"/>.
/// </summary>
public sealed class CrcReferencesTimer(IReferencesTimerRunner referencesTimerRunner)
{
    /// <summary>
    /// Executes one CRC references full-sync cycle using the configured timer schedule.
    /// </summary>
    /// <param name="timer">Timer trigger metadata for this invocation.</param>
    /// <param name="ct">Cancellation token propagated by the function host.</param>
    [Function("CrcReferencesFullSyncTimer")]
    public Task RunAsync([TimerTrigger("%CrcReferencesFullSyncSchedule%")] TimerInfo timer, CancellationToken ct)
    {
        return referencesTimerRunner.RunAsync(LobName.Crc, timer, ct);
    }
}
