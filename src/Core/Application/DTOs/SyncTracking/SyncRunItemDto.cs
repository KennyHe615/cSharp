using Application.Enums;


namespace Application.DTOs.SyncTracking;

/// <summary>
/// Persistence projection for one claimable sync run item.
/// A run item can represent either a generic stage marker or a page-level work item within a sync run.
/// </summary>
public sealed class SyncRunItemDto
{
    /// <summary>
    /// Internal relational key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Parent physical sync run identifier.
    /// </summary>
    public long RunId { get; set; }

    /// <summary>
    /// Logical stage or item type, such as Fetch, Normalize, Upsert, or Dispatch.
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// Optional generic selector token for non-page work items, such as a scope key or slice key.
    /// Page-based work items must leave this value null and use <see cref="PageNumber"/> instead.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Optional one-based page number for page-level work items.
    /// Generic stage markers must leave this value null and use <see cref="Cursor"/> instead.
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Current lifecycle state for this run item.
    /// </summary>
    public SyncRunStatus Status { get; set; }

    /// <summary>
    /// Optional failure reason captured when the item fails or is canceled.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Logical worker identifier that currently owns the lease for this page item.
    /// Null when the item is not currently claimed or when the item is a generic stage marker.
    /// </summary>
    public string? ClaimedBy { get; set; }

    /// <summary>
    /// Lease ownership token for the current page claim.
    /// A new token should be generated whenever a worker successfully acquires or reacquires the lease.
    /// Null when the item is not currently claimed or when the item is a generic stage marker.
    /// </summary>
    public Guid? LeaseToken { get; set; }

    /// <summary>
    /// Eastern application timestamp when the current lease was acquired.
    /// Null when the item is not currently claimed.
    /// </summary>
    public DateTimeOffset? ClaimedAtEastern { get; set; }

    /// <summary>
    /// Eastern application timestamp when the current lease expires.
    /// Null when the item is not currently claimed.
    /// </summary>
    public DateTimeOffset? ClaimExpiresAtEastern { get; set; }

    /// <summary>
    /// Number of claim attempts that have been made for this item.
    /// This includes retries after failures or expired leases.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Eastern application timestamp of the latest heartbeat recorded for the active claim.
    /// Null when the item is not currently claimed or heartbeat tracking is unused.
    /// </summary>
    public DateTimeOffset? LastHeartbeatAtEastern { get; set; }
}
