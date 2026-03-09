using Application.Enums;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Logical sync request record scoped within one LOB database. (what should be executed).
/// Lifecycle details are tracked in SyncRunEntity.
/// </summary>
public sealed class SyncRequestEntity : Audit
{
    public long Id { get; set; }

    // Business-facing category: UsersDetails / ConversationsDetails / References...
    public SyncCategory Category { get; set; }

    // Execution mode: Incremental / Recovery
    public SyncMode Mode { get; set; }

    public string? Interval { get; set; }

    public int? PageNumber { get; set; }

    // External provider identifier from HTTP POST body (used to query Genesys API).
    public string? GenesysJobId { get; set; }

    // Persisted for unique index. Private setter prevents accidental drift.
    // {Category}|{Mode}|{Interval or -}|{PageNumber or -}|{GenesysJobId or -}
    public string ScopeKey { get; private set; } = string.Empty;

    // Internal pointer to latest/current execution run.
    public long? CurrentRunId { get; set; }

    public SyncRunEntity? CurrentRun { get; set; }

    public ICollection<SyncRunEntity> Runs { get; set; } = [];

    public static string BuildScopeKey(SyncCategory category,
                                       SyncMode mode,
                                       string? interval,
                                       int? pageNumber,
                                       string? genesysJobId)
    {
        string intervalPart = string.IsNullOrWhiteSpace(interval) ? "-" : interval.Trim();
        string pagePart = pageNumber.HasValue ? pageNumber.Value.ToString() : "-";
        string genesysPart = string.IsNullOrWhiteSpace(genesysJobId) ? "-" : genesysJobId.Trim();

        return $"{category}|{mode}|{intervalPart}|{pagePart}|{genesysPart}";
    }

    public void RebuildScopeKey()
    {
        ScopeKey = BuildScopeKey(Category,
                                 Mode,
                                 Interval,
                                 PageNumber,
                                 GenesysJobId);
    }
}
