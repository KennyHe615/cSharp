using Application.Enums;


namespace Application.DTOs.SyncTracking;

public sealed class SyncRequestDto
{
    public long Id { get; set; }

    public SyncCategory Category { get; set; }

    public SyncMode Mode { get; set; }

    public string? Interval { get; set; }

    public int? PageNumber { get; set; }

    public string? GenesysJobId { get; set; }

    public string ScopeKey { get; set; } = string.Empty;

    public long? CurrentRunId { get; set; }
}
