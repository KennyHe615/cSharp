using Application.Enums;


namespace Application.Abstractions.Orchestration;

/// <summary>
/// Routes one sync run execution to the appropriate category/mode implementation.
/// </summary>
public interface ISyncExecutionDispatcher
{
    /// <summary>
    /// Executes a sync run for the provided scope selectors.
    /// Implementations are responsible for route selection and execution-stage tracking.
    /// </summary>
    /// <param name="runId">Physical run identifier.</param>
    /// <param name="category">Business sync category.</param>
    /// <param name="mode">Execution mode.</param>
    /// <param name="interval">Optional interval selector.</param>
    /// <param name="pageNumber">Optional page selector.</param>
    /// <param name="genesysJobId">Optional external provider job id.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(long runId,
                      SyncCategory category,
                      SyncMode mode,
                      string? interval,
                      int? pageNumber,
                      string? genesysJobId,
                      CancellationToken ct);
}
