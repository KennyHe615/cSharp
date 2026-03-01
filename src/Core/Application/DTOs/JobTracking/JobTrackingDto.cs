using Application.Enums;


namespace Application.DTOs.JobTracking;

public sealed class JobTrackingDto
{
    public long Id { get; set; }

    public SyncDataType Category { get; set; }

    public string? Interval { get; set; }

    public string? JobId { get; set; }

    public bool IsIncrementalCompleted { get; set; }

    public bool IsRecoveryCompleted { get; set; }
}
