using SharedKernel.Time;


namespace Application.Contracts.InternalApis.Recovery;

/// <summary>
/// Response returned after a recovery intake request has been created or reused.
/// </summary>
/// <param name="Success">Indicates whether the recovery intake request was accepted successfully.</param>
/// <param name="Message">Human-readable result message.</param>
/// <param name="Data">Detailed recovery intake request resolution payload.</param>
public sealed record CreateRecoveryRequestResponse(bool Success,
                                                   string Message,
                                                   CreateRecoveryRequestResponseData Data);

/// <summary>
/// Detailed payload describing the resolved recovery intake request.
/// </summary>
/// <param name="RequestId">Client-facing recovery intake request identifier.</param>
/// <param name="RequestAction">Resolution action applied by persistence, such as Created or ReusedActive.</param>
/// <param name="Lob">Line-of-business value associated with the request.</param>
/// <param name="Category">Recovery category requested by the client.</param>
/// <param name="Interval">Optional original interval submitted for interval-based recovery.</param>
/// <param name="GenesysJobId">
/// Optional Genesys job identifier submitted for job-based recovery.
/// Supported only for <see cref="RecoveryCategory.ConversationsDetails"/> recovery.
/// </param>
public sealed record CreateRecoveryRequestResponseData(Guid RequestId,
                                                       string RequestAction,
                                                       string Lob,
                                                       string Category,
                                                       UtcInterval? Interval,
                                                       string? GenesysJobId);
