using SharedKernel.Time;


namespace Application.Contracts.InternalApis.Recovery;

public sealed class RecoveryRequest
{
    public string Lob { get; set; } = string.Empty;

    public RecoveryCategory? Category { get; set; }

    public UtcInterval? Interval { get; set; }

    public string? JobId { get; set; }
}
