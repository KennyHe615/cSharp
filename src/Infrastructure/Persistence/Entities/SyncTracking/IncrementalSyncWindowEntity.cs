using Application.Enums;


namespace Infrastructure.Persistence.Entities.SyncTracking;

/// <summary>
/// Durable incremental scheduling cursor for one analytics category within the current LOB database.
/// This entity is independent from <see cref="SyncRequestEntity"/> and is used only to atomically
/// reserve the next incremental window before normal sync execution begins.
/// </summary>
public sealed class IncrementalSyncWindowEntity : Audit
{
    /// <summary>
    /// Internal relational key used for joins and persistence identity.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Analytics category whose incremental window is being tracked.
    /// One row is expected per category within the current LOB database.
    /// </summary>
    public SyncAnalyticsCategory Category { get; set; }

    /// <summary>
    /// Earliest UTC timestamp that has not yet been reserved for incremental processing.
    /// When no historical reservation exists, this should be initialized to the start of the current Eastern day in UTC.
    /// </summary>
    public DateTimeOffset NextIntervalStartUtc { get; set; }

    /// <summary>
    /// Most recent reserved window start in UTC.
    /// This is useful for diagnostics and operational tracing.
    /// </summary>
    public DateTimeOffset? LastReservedStartUtc { get; set; }

    /// <summary>
    /// Most recent reserved window end in UTC.
    /// This is useful for diagnostics and operational tracing.
    /// </summary>
    public DateTimeOffset? LastReservedEndUtc { get; set; }

    /// <summary>
    /// Optimistic concurrency token used to protect atomic reservation updates across overlapping workers.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}
