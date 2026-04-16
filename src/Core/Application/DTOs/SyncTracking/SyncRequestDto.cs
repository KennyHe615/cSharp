using Application.Enums;


namespace Application.DTOs.SyncTracking;

/// <summary>
/// DTO representation of sync request persistence model.
/// </summary>
public sealed class SyncRequestDto
{
    public long Id { get; set; }

    public string Category { get; set; } = string.Empty;

    public SyncMode Mode { get; set; }

    public string? Interval { get; set; }

    public int? PageNumber { get; set; }

    public string? GenesysJobId { get; set; }

    public string ScopeKey { get; set; } = string.Empty;

    public long? CurrentRunId { get; set; }
}
