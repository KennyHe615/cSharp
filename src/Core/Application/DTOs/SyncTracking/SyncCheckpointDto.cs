using Application.Enums;


namespace Application.DTOs.SyncTracking;

public sealed class SyncCheckpointDto
{
    public long Id { get; set; }

    public long RunId { get; set; }

    public string Step { get; set; } = string.Empty;

    public string Cursor { get; set; } = string.Empty;

    public SyncRunStatus Status { get; set; }

    public string? FailureReason { get; set; }
}
