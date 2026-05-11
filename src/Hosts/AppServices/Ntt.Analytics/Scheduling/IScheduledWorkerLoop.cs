namespace Ntt.Analytics.Scheduling;

/// <summary>
/// Represents one scheduled worker loop owned by the NTT analytics host.
/// </summary>
public interface IScheduledWorkerLoop
{
    /// <summary>
    /// Runs the scheduled worker loop until the host is stopped.
    /// </summary>
    /// <param name="ct">Cancellation token propagated by the host.</param>
    Task RunAsync(CancellationToken ct);
}
