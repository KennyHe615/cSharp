using Microsoft.Azure.Functions.Worker;

using SharedKernel.Lobs;


namespace FunctionApps.Timer;

/// <summary>
/// Executes one full references-domain timer cycle for a specific Line of Business (LOB).
/// </summary>
public interface IReferencesTimerRunner
{
    /// <summary>
    /// Runs full-sync orchestration for all supported references categories under the specified LOB.
    /// </summary>
    /// <param name="lob">Target LOB to execute against.</param>
    /// <param name="timer">Timer trigger metadata for this invocation.</param>
    /// <param name="ct">Cancellation token propagated from the function host.</param>
    Task RunAsync(LobName lob, TimerInfo timer, CancellationToken ct);
}
