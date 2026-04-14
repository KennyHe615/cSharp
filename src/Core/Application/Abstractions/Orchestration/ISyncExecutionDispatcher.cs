using Application.Enums;


namespace Application.Abstractions.Orchestration;

/// <summary>
/// Routes a sync execution request to the appropriate category and mode pipeline.
/// </summary>
public interface ISyncExecutionDispatcher
{
    /// <summary>
    /// Executes one sync run for the provided scope selectors.
    /// </summary>
    /// <param name="runId">The sync run identifier.</param>
    /// <param name="category">The sync category to dispatch.</param>
    /// <param name="mode">The sync mode for the selected category.</param>
    /// <param name="interval">An optional interval selector for interval-based scopes.</param>
    /// <param name="pageNumber">An optional page selector for page-based scopes.</param>
    /// <param name="genesysJobId">An optional external job identifier from Genesys.</param>
    /// <param name="ct">The cancellation token for the operation.</param>
    Task ExecuteAsync(long runId,
                      string category,
                      SyncMode mode,
                      string? interval,
                      int? pageNumber,
                      string? genesysJobId,
                      CancellationToken ct);
}
