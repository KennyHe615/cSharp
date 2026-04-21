using SharedKernel.Time;


namespace Application.Contracts.InternalApis.Recovery;

/// <summary>
/// Response returned after a recovery request has been created, reused, or reopened.
/// </summary>
/// <param name="Success">Indicates whether the recovery request was accepted successfully.</param>
/// <param name="Message">Human-readable result message.</param>
/// <param name="Data">Detailed recovery request resolution payload.</param>
public sealed record CreateRecoveryRequestResponse(bool Success,
                                                   string Message,
                                                   CreateRecoveryRequestResponseData Data);

/// <summary>
/// Detailed payload describing the resolved recovery request.
/// </summary>
/// <param name="RequestId">Client-facing recovery request identifier.</param>
/// <param name="RequestAction">Resolution action applied by persistence, such as Created or ReusedActive.</param>
/// <param name="Lob">Line-of-business value associated with the request.</param>
/// <param name="Category">Recovery category requested by the client.</param>
/// <param name="Interval">Optional interval selected for interval-based recovery.</param>
/// <param name="GenesysJobId">Optional Genesys job identifier selected for job-based recovery.</param>
public sealed record CreateRecoveryRequestResponseData(Guid RequestId,
                                                       string RequestAction,
                                                       string Lob,
                                                       string Category,
                                                       UtcInterval? Interval,
                                                       string? GenesysJobId);
