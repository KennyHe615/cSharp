using Application.Enums;


namespace Application.DTOs.Recovery;

/// <summary>
/// Persistence projection for one user-submitted analytics recovery intake request.
/// This model represents the validated request submitted by the HTTP recovery entry point.
/// </summary>
public sealed class AnalyticsRecoveryRequestDto
{
    /// <summary>
    /// Internal database identifier.
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
    /// Intake lifecycle state for the recovery request.
    /// </summary>
    public AnalyticsRecoveryRequestStatus Status { get; set; }

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
}

/// <summary>
/// Resolve outcome for recovery intake create-or-get operations.
/// </summary>
public sealed class AnalyticsRecoveryRequestResolveResult
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public long Id { get; init; }

    /// <summary>
    /// Client-facing immutable request identifier.
    /// </summary>
    public Guid PublicId { get; init; }

    /// <summary>
    /// Resolution action applied by persistence logic.
    /// </summary>
    public AnalyticsRecoveryRequestResolveAction RequestAction { get; init; }
}

/// <summary>
/// Action chosen when resolving a recovery intake create operation.
/// </summary>
public enum AnalyticsRecoveryRequestResolveAction
{
    Created = 1,
    ReusedActive = 2
}
