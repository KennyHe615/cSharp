using Application.Enums;

using SharedKernel.Sync;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Logical sync request record scoped within one LOB database (what should be executed).
/// Execution attempts and step-level lifecycle details are tracked in <see cref="SyncRunEntity"/>.
/// </summary>
public sealed class SyncRequestEntity : Audit
{
    /// <summary>
    /// Internal relational key used for joins and FK relationships.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Client-facing immutable identifier.
    /// </summary>
    public Guid PublicId { get; set; }

    /// <summary>
    /// Business-facing sync category, such as Queues, UsersDetails or ConversationsDetails.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Execution mode for this logical request.
    /// References Domain uses Full.
    /// Analytics Domain uses Incremental and Recovery.
    /// </summary>
    public SyncMode Mode { get; set; }

    /// <summary>
    /// Request-level lifecycle state used by recovery resolve rules.
    /// </summary>
    public SyncRequestStatus Status { get; set; } = SyncRequestStatus.Pending;

    /// <summary>
    /// Number of reopen operations applied to this request.
    /// </summary>
    public int ReopenCount { get; set; }

    /// <summary>
    /// Optional interval selector persisted as part of the request scope.
    /// </summary>
    public string? Interval { get; set; }

    /// <summary>
    /// Optional page selector persisted as part of the request scope.
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Optional provider job identifier persisted as part of the request scope for applicable categories.
    /// </summary>
    public string? GenesysJobId { get; set; }

    /// <summary>
    /// Canonical persisted scope identity.
    /// Format: {Category}|{Mode}|{Interval or -}|{PageNumber or -}|{GenesysJobId or -}
    /// </summary>
    public string ScopeKey { get; private set; } = string.Empty;

    /// <summary>
    /// Internal pointer to the latest or current execution run for this request.
    /// </summary>
    public long? CurrentRunId { get; set; }

    /// <summary>
    /// Navigation reference to the latest or current execution run.
    /// </summary>
    public SyncRunEntity? CurrentRun { get; set; }

    /// <summary>
    /// All physical execution runs created for this logical request.
    /// </summary>
    public ICollection<SyncRunEntity> Runs { get; set; } = [];

    /// <summary>
    /// Rebuilds the canonical scope key from the current request selectors.
    /// </summary>
    public void RebuildScopeKey()
    {
        ScopeKey = SyncScopeKeyFormatter.Format(Category,
                                                Mode.ToString(),
                                                Interval,
                                                PageNumber,
                                                GenesysJobId);
    }
}
