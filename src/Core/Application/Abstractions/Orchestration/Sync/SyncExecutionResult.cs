namespace Application.Abstractions.Orchestration.Sync;

/// <summary>
/// Result returned by a sync execution pipeline.
/// </summary>
/// <param name="CompletedWithRecoveryItems">
/// <c>true</c> when execution succeeded and emitted recovery items; otherwise <c>false</c>.
/// </param>
/// <param name="Failed">
/// <c>true</c> when execution reached a known terminal failure that should finalize the run as failed.
/// </param>
/// <param name="FailureReason">
/// Optional failure reason for known terminal failures.
/// </param>
public sealed record SyncExecutionResult(bool CompletedWithRecoveryItems,
                                         bool Failed = false,
                                         string? FailureReason = null);
