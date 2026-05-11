namespace Application.Enums;

/// <summary>
/// Lifecycle states for a user-submitted analytics recovery intake request.
/// These records are planned into executable sync_request rows before sync tracking runs them.
/// </summary>
public enum AnalyticsRecoveryRequestStatus
{
    Pending = 1,
    Running = 2,
    Completed = 3,
    Failed = 4,
    Canceled = 5
}
