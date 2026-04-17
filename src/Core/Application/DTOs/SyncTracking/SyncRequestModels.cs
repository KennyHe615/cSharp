using Application.Enums;


namespace Application.DTOs.SyncTracking;

/// <summary>
/// Persistence projection for a logical sync request.
/// </summary>
public sealed class SyncRequestDto
{
    /// <summary>
    /// Internal database identifier (not client-facing).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Client-facing immutable request identifier.
    /// </summary>
    public Guid PublicId { get; set; }

    /// <summary>
    /// Sync category token (e.g., analytics domain category).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Execution mode for this request.
    /// </summary>
    public SyncMode Mode { get; set; }

    /// <summary>
    /// Request-level lifecycle state used for dedupe/reuse decisions.
    /// </summary>
    public SyncRequestStatus Status { get; set; }

    /// <summary>
    /// Number of reopen operations performed on this request.
    /// </summary>
    public int ReopenCount { get; set; }

    /// <summary>
    /// Optional interval selector.
    /// </summary>
    public string? Interval { get; set; }

    /// <summary>
    /// Optional page selector.
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Optional external provider job identifier.
    /// </summary>
    public string? GenesysJobId { get; set; }

    /// <summary>
    /// Canonical scope identity key.
    /// </summary>
    public string ScopeKey { get; set; } = string.Empty;

    /// <summary>
    /// Internal pointer to current run, when present.
    /// </summary>
    public long? CurrentRunId { get; set; }
}

/// <summary>
/// Resolve outcome for create-or-get request operations.
/// </summary>
public sealed class SyncRequestResolveResult
{
    /// <summary>
    /// Internal database identifier (used by orchestration).
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Client-facing immutable request identifier.
    /// </summary>
    public Guid PublicId { get; init; }

    /// <summary>
    /// Resolution action applied by persistence logic.
    /// </summary>
    public SyncRequestResolveAction RequestAction { get; init; }
}

/// <summary>
/// Action chosen when resolving a request create operation.
/// </summary>
public enum SyncRequestResolveAction
{
    Created = 1,
    ReusedFailed = 2,
    ReusedActive = 3
}
