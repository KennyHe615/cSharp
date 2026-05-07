namespace Application.Enums;

/// <summary>
/// Lifecycle states for one physical sync execution attempt.
/// A sync run is one execution attempt for a logical sync request.
/// </summary>
public enum SyncRunStatus
{
    Pending = 1,                   // Created but not yet claimed/executing
    Running = 2,                   // Actively executing
    Completed = 3,                 // Finished successfully with no recovery items emitted
    CompletedWithRecoveryItems = 4,// Finished successfully and emitted recovery items
    Failed = 5,                    // Finished with error
    Superseded = 6,                // Replaced by a newer run of the same scope
    Canceled = 7                   // Canceled by host/user shutdown or control action
}
