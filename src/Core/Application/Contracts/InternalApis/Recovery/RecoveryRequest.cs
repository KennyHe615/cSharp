using SharedKernel.Time;


namespace Application.Contracts.InternalApis.Recovery;

/// <summary>
/// Public request payload for creating a recovery request.
/// </summary>
public sealed class RecoveryRequest
{
    /// <summary>
    /// Line-of-business identifier for the recovery request.
    /// </summary>
    public string Lob { get; set; } = string.Empty;

    /// <summary>
    /// Recovery category to execute.
    /// </summary>
    public RecoveryCategory? Category { get; set; }

    /// <summary>
    /// Optional UTC interval to recover.
    /// </summary>
    public UtcInterval? Interval { get; set; }

    /// <summary>
    /// Optional Genesys job identifier to recover.
    /// Supported only for <see cref="RecoveryCategory.ConversationsDetails"/>.
    /// </summary>
    public string? GenesysJobId { get; set; }
}
