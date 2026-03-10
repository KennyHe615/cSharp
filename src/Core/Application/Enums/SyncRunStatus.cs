namespace Application.Enums;

/// <summary>
/// Lifecycle states for one distributed sync execution run.
/// </summary>
public enum SyncRunStatus
{
    Pending = 1,   // Created but not yet claimed/executing
    Running = 2,   // Actively executing
    Completed = 3, // Finished successfully
    Failed = 4,    // Finished with error
    Superseded = 5,// Replaced by a newer run of the same scope
    Canceled = 6   // Canceled by host/user shutdown or control action
}
