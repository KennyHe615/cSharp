using Application.Common.Enums;


namespace Infrastructure.Persistence.Entities.JobTracking;

public class JobTrackingEntity : Audit
{
    public long Id { get; set; }

    public SyncCategory Category { get; set; }

    public string? Interval { get; set; }

    public int? PageNumber { get; set; }

    public string? JobId { get; set; }

    public bool IsIncrementalCompleted { get; set; } = false;

    public bool IsRecoveryCompleted { get; set; } = false;
}
