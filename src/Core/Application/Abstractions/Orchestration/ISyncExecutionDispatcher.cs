using Application.Enums;


namespace Application.Abstractions.Orchestration;

/// <summary>
/// Routes a sync execution request to the appropriate category and mode pipeline.
/// </summary>
public interface ISyncExecutionDispatcher
{
    /// <summary>
    /// Executes one sync run for the provided scope selectors and returns the execution outcome.
    /// </summary>
    /// <param name="runId">The sync run identifier.</param>
    /// <param name="category">The sync category to dispatch.</param>
    /// <param name="mode">The sync mode for the selected category.</param>
    /// <param name="interval">An optional interval selector for interval-based scopes.</param>
    /// <param name="pageNumber">An optional page selector for page-based scopes.</param>
    /// <param name="genesysJobId">An optional external job identifier from Genesys.</param>
    /// <param name="ct">The cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="SyncExecutionResult"/> describing whether execution completed normally
    /// or completed while emitting recovery items.
    /// </returns>
    Task<SyncExecutionResult> ExecuteAsync(long runId,
                                           string category,
                                           SyncMode mode,
                                           string? interval,
                                           int? pageNumber,
                                           string? genesysJobId,
                                           CancellationToken ct);
}

/// <summary>
/// Result returned by a sync execution pipeline.
/// </summary>
/// <param name="CompletedWithRecoveryItems">
/// <c>true</c> when execution succeeded and emitted recovery items; otherwise <c>false</c>.
/// </param>
public sealed record SyncExecutionResult(bool CompletedWithRecoveryItems);
