using Application.Enums;


namespace Application.Abstractions.Orchestration;

/// <summary>
/// Executes one analytics sync run for a single analytics category.
/// Implementations own category-specific API, normalization, persistence, and recovery behavior.
/// </summary>
public interface IAnalyticsSyncExecutor
{
    /// <summary>
    /// Gets the analytics category handled by this executor.
    /// </summary>
    SyncAnalyticsCategory Category { get; }

    /// <summary>
    /// Executes one analytics sync run for the supplied sync scope.
    /// </summary>
    /// <param name="runId">Physical sync run identifier.</param>
    /// <param name="mode">Sync mode for the executable request.</param>
    /// <param name="interval">Optional UTC interval selector for interval-based analytics requests.</param>
    /// <param name="pageNumber">Optional one-based page selector for page-based recovery requests.</param>
    /// <param name="genesysJobId">Optional Genesys job id for supported analytics categories.</param>
    /// <param name="ct">Cancellation token propagated by the sync runner.</param>
    /// <returns>The analytics execution result for the run.</returns>
    Task<SyncExecutionResult> ExecuteAsync(long runId,
                                           SyncMode mode,
                                           string? interval,
                                           int? pageNumber,
                                           string? genesysJobId,
                                           CancellationToken ct);
}
