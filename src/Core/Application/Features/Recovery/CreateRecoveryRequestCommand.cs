using Application.Contracts.InternalApis.Recovery;
using Application.Mediator;

using SharedKernel.Lobs;
using SharedKernel.Time;


namespace Application.Features.Recovery;

/// <summary>
/// Represents a request to create a recovery job tracking record.
/// </summary>
/// <param name="Lob">Line-of-business identifier for the recovery request.</param>
/// <param name="Category">Recovery category to execute.</param>
/// <param name="Interval">Optional UTC interval to recover.</param>
/// <param name="JobId">Optional existing job identifier to recover.</param>
public sealed record CreateRecoveryRequestCommand(LobName Lob,
                                                  RecoveryCategory Category,
                                                  UtcInterval? Interval,
                                                  string? JobId) : IRequest<CreateRecoveryRequestResponse>;
