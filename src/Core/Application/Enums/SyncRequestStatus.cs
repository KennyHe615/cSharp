namespace Application.Enums;

/// <summary>
/// Lifecycle states for one logical sync request record.
/// Distinct from <see cref="SyncRunStatus"/>, which tracks each execution attempt.
/// </summary>
public enum SyncRequestStatus
{
    Pending = 1,                   // Request exists and is waiting to be executed
    Running = 2,                   // Current run for this request is in progress
    Completed = 3,                 // Latest run finished successfully with no recovery items emitted
    CompletedWithRecoveryItems = 4,// Latest run finished successfully and emitted recovery items
    Failed = 5,                    // Latest run finished with error
    Canceled = 6                   // Latest run was canceled
}
