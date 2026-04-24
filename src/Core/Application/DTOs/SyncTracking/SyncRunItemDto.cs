using Application.Enums;


namespace Application.DTOs.SyncTracking;

/// <summary>
/// Persistence projection for one claimable sync run item.
/// </summary>
public sealed class SyncRunItemDto
{
    /// <summary>
    /// Internal relational key used for joins and lookups.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Parent physical sync run identifier.
    /// </summary>
    public long RunId { get; set; }

    /// <summary>
    /// Logical stage or item type.
    /// </summary>
    public string Step { get; set; } = string.Empty;

    /// <summary>
    /// Item cursor or selector token, such as page number or slice key.
    /// </summary>
    public string Cursor { get; set; } = string.Empty;

    /// <summary>
    /// Current lifecycle state for this run item.
    /// </summary>
    public SyncRunStatus Status { get; set; }

    /// <summary>
    /// Optional failure reason captured for failed or canceled items.
    /// </summary>
    public string? FailureReason { get; set; }
}
