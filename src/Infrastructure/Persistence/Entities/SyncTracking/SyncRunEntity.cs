using Application.Enums;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Physical execution run for a sync request.
/// This is the single source of truth for one execution attempt lifecycle.
/// </summary>
public sealed class SyncRunEntity : Audit
{
    /// <summary>
    /// Internal relational key used for joins and FK relationships.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Parent logical sync request identifier.
    /// </summary>
    public long RequestId { get; set; }

    /// <summary>
    /// Current lifecycle state of this execution attempt.
    /// </summary>
    public SyncRunStatus Status { get; set; } = SyncRunStatus.Pending;

    /// <summary>
    /// Set only when this run is replaced by a newer run of the same request scope.
    /// </summary>
    public long? SupersededByRunId { get; set; }

    /// <summary>
    /// Monotonic attempt number within the parent request scope.
    /// </summary>
    public int AttemptNo { get; set; } = 1;

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

    /// <summary>
    /// Navigation reference to the parent logical sync request.
    /// </summary>
    public SyncRequestEntity Request { get; set; } = null!;

    /// <summary>
    /// Navigation reference to the newer run that superseded this run.
    /// </summary>
    public SyncRunEntity? SupersededByRun { get; set; }

    /// <summary>
    /// Older runs that were superseded by this run.
    /// </summary>
    public ICollection<SyncRunEntity> SupersededRuns { get; set; } = [];

    /// <summary>
    /// Claimable work items that belong to this physical run.
    /// </summary>
    public ICollection<SyncRunItemEntity> RunItems { get; set; } = [];
}
