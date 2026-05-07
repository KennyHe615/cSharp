using Application.Enums;

using SharedKernel.Sync;


namespace Infrastructure.Persistence.Entities.Recovery;

/// <summary>
/// User-submitted analytics recovery intake request.
/// These rows are accepted by the HTTP recovery boundary and later planned into executable sync_request rows.
/// </summary>
public sealed class AnalyticsRecoveryRequestEntity : Audit
{
    /// <summary>
    /// Scope-mode token used when building active-intake dedupe keys.
    /// </summary>
    public const string ScopeMode = "RECOVERY_INTAKE";

    /// <summary>
    /// Internal relational key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Client-facing immutable request identifier.
    /// </summary>
    public Guid PublicId { get; set; }

    /// <summary>
    /// Analytics recovery category, such as UsersDetails or ConversationsDetails.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Intake lifecycle state for this recovery request.
    /// </summary>
    public AnalyticsRecoveryRequestStatus Status { get; set; } = AnalyticsRecoveryRequestStatus.Pending;

    /// <summary>
    /// Optional original UTC interval submitted by the recovery caller.
    /// The interval may need planning before executable sync_request rows are created.
    /// </summary>
    public string? Interval { get; set; }

    /// <summary>
    /// Optional Genesys job identifier for supported analytics categories.
    /// </summary>
    public string? GenesysJobId { get; set; }

    /// <summary>
    /// Latest failure reason captured while accepting or planning the intake request.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Canonical persisted scope identity used to deduplicate active intake requests.
    /// Format: {Category}|RECOVERY_INTAKE|{Interval or -}|-|{GenesysJobId or -}
    /// </summary>
    public string ScopeKey { get; private set; } = string.Empty;

    /// <summary>
    /// Rebuilds the canonical active-intake scope key from the current request selectors.
    /// </summary>
    public void RebuildScopeKey()
    {
        ScopeKey = SyncScopeKeyFormatter.Format(Category,
                                                ScopeMode,
                                                Interval,
                                                null,
                                                GenesysJobId);
    }
}
