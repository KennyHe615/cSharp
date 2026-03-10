namespace Application.Abstractions.Orchestration;

/// <summary>
/// Orchestrates execution of one logical sync request scope through run lifecycle states.
/// </summary>
public interface ISyncRequestRunner
{
    /// <summary>
    /// Executes a sync request:
    /// loads request metadata, starts a run, dispatches execution when still current,
    /// and applies terminal run status (Completed, Failed, or Canceled).
    /// </summary>
    /// <param name="requestId">Logical sync request id to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(long requestId, CancellationToken ct);
}
