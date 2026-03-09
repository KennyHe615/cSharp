using Application.Enums;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Physical execution run for a SyncRequest.
/// This is the single source of truth for lifecycle status.
/// </summary>
public sealed class SyncRunEntity : Audit
{
    public long Id { get; set; }

    public long RequestId { get; set; }

    public SyncRunStatus Status { get; set; } = SyncRunStatus.Pending;

    // Set only when this run is replaced by a newer run of the same scope.
    public long? SupersededByRunId { get; set; }

    public int AttemptNo { get; set; } = 1;

    public DateTimeOffset? RunStartedAt { get; set; }

    public DateTimeOffset? RunCompletedAt { get; set; }

    public string? FailureReason { get; set; }

    public SyncRequestEntity Request { get; set; } = null!;

    public SyncRunEntity? SupersededByRun { get; set; }

    public ICollection<SyncRunEntity> SupersededRuns { get; set; } = [];

    public ICollection<SyncCheckpointEntity> Checkpoints { get; set; } = [];
}
