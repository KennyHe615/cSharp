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

    // Business-facing category within the domain: User / Queue / UsersDetails / etc.
    public string Category { get; set; } = string.Empty;

    // Execution mode: Incremental / Recovery
    public SyncMode Mode { get; set; }

    /// <summary>
    /// Request-level lifecycle state used by recovery resolve rules.
    /// </summary>
    public SyncRequestStatus Status { get; set; } = SyncRequestStatus.Pending;

    /// <summary>
    /// Number of reopen operations applied to this request.
    /// </summary>
    public int ReopenCount { get; set; }

    public string? Interval { get; set; }

    public int? PageNumber { get; set; }

    // External provider identifier from HTTP POST body (used to query Genesys API).
    public string? GenesysJobId { get; set; }

    // Persisted scope identity. Private setter prevents accidental drift.
    // {Category}|{Mode}|{Interval or -}|{PageNumber or -}|{GenesysJobId or -}
    public string ScopeKey { get; private set; } = string.Empty;

    // Internal pointer to latest/current execution run.
    public long? CurrentRunId { get; set; }

    public SyncRunEntity? CurrentRun { get; set; }

    public ICollection<SyncRunEntity> Runs { get; set; } = [];

    public void RebuildScopeKey()
    {
        ScopeKey = SyncScopeKeyFormatter.Format(Category,
                                                Mode.ToString(),
                                                Interval,
                                                PageNumber,
                                                GenesysJobId);
    }
}
