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
/// <param name="GenesysJobId">
/// Optional existing Genesys job identifier to recover.
/// Supported only for <see cref="RecoveryCategory.ConversationsDetails"/>.
/// </param>
public sealed record CreateRecoveryRequestCommand(LobName Lob,
                                                  RecoveryCategory Category,
                                                  UtcInterval? Interval,
                                                  string? GenesysJobId) : IRequest<CreateRecoveryRequestResponse>;
