using Application.Enums;


namespace Application.DTOs.SyncTracking;

public sealed class SyncRunDto
{
    public long Id { get; set; }

    public long RequestId { get; set; }

    public SyncRunStatus Status { get; set; }

    public long? SupersededByRunId { get; set; }

    public int AttemptNo { get; set; }

    public DateTimeOffset? RunStartedAt { get; set; }

    public DateTimeOffset? RunCompletedAt { get; set; }

    public string? FailureReason { get; set; }
}
