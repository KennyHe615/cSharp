using Application.Enums;


namespace Application.Abstractions.Orchestration;

/// <summary>
/// Executor for References full-sync categories.
/// Called by the top-level sync dispatcher after category/mode resolution.
/// </summary>
public interface IReferencesSyncOrchestrator
{
    /// <summary>
    /// Executes one References full-sync category for the given run.
    /// </summary>
    /// <param name="runId">Physical run identifier.</param>
    /// <param name="category">References category to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(long runId, SyncReferenceCategory category, CancellationToken ct = default);
}
