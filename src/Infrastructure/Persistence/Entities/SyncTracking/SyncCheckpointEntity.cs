using Application.Enums;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Checkpoint record for resumable sync execution.
/// </summary>
public sealed class SyncCheckpointEntity : Audit
{
    public long Id { get; set; }

    public long RunId { get; set; }

    // Logical stage: Fetch / Normalize / Upsert / Finalize
    public string Step { get; set; } = string.Empty;

    // Cursor token for paging/slicing resume (page number, API cursor, etc.).
    public string Cursor { get; set; } = string.Empty;

    // Pending, Running, Completed, Failed, Superseded, Canceled
    public SyncRunStatus Status { get; set; } = SyncRunStatus.Pending;

    public string? FailureReason { get; set; }

    public SyncRunEntity Run { get; set; } = null!;
}
