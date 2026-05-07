namespace Application.Abstractions.Orchestration;

/// <summary>
/// Orchestrates execution of one logical sync request scope through run lifecycle states.
/// </summary>
public interface ISyncRequestRunner
{
    /// <summary>
    /// Executes a sync request:
    /// loads request metadata, starts a run, dispatches execution when still current,
    /// applies terminal run status, and returns the execution outcome.
    /// </summary>
    /// <param name="requestId">Logical sync request id to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="SyncExecutionResult"/> describing whether execution completed normally
    /// or completed while emitting recovery items.
    /// </returns>
    Task<SyncExecutionResult> ExecuteAsync(long requestId, CancellationToken ct);
}
