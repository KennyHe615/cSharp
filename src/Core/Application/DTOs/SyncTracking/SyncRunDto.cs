using Application.Enums;


namespace Application.DTOs.SyncTracking;

/// <summary>
/// Persistence projection for one physical sync execution attempt.
/// </summary>
public sealed class SyncRunDto
{
    /// <summary>
    /// Internal relational key used for joins and lookups.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Parent logical sync request identifier.
    /// </summary>
    public long RequestId { get; set; }

    /// <summary>
    /// Current lifecycle state of this execution attempt.
    /// </summary>
    public SyncRunStatus Status { get; set; }

    /// <summary>
    /// Newer run identifier when this run has been superseded.
    /// </summary>
    public long? SupersededByRunId { get; set; }

    /// <summary>
    /// Monotonic attempt number within the parent request scope.
    /// </summary>
    public int AttemptNo { get; set; }

    /// <summary>
    /// Eastern application timestamp when execution entered the running state.
    /// </summary>
    public DateTimeOffset? RunStartedAtEastern { get; set; }

    /// <summary>
    /// Eastern application timestamp when execution reached a terminal state.
    /// </summary>
    public DateTimeOffset? RunCompletedAtEastern { get; set; }

    /// <summary>
    /// Optional run-level failure summary.
    /// </summary>
    public string? FailureReason { get; set; }
}
