using Application.Enums;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Claimable execution item for a sync run.
/// A run item represents one page, slice, or step-level work unit within a physical sync run.
/// </summary>
public sealed class SyncRunItemEntity : Audit
{
    /// <summary>
    /// Internal relational key used for joins and FK relationships.
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
    /// Item cursor or selector token for claim/retry semantics, such as page number or slice key.
    /// </summary>
    public string Cursor { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle state for this run item.
    /// </summary>
    public SyncRunStatus Status { get; set; } = SyncRunStatus.Pending;

    /// <summary>
    /// Optional failure reason captured when the item fails or is canceled.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Navigation reference to the parent physical sync run.
    /// </summary>
    public SyncRunEntity Run { get; set; } = null!;
}
