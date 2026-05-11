using Application.Contracts.InternalApis.Recovery;
using Application.Mediator;

using SharedKernel.Lobs;
using SharedKernel.Time;


namespace Application.Features.Recovery;

/// <summary>
/// Represents a request to create or reuse an analytics recovery intake record.
/// The intake record is later materialized into executable sync_request work by the scheduled planner.
/// </summary>
/// <param name="Lob">Line-of-business identifier for the recovery intake request.</param>
/// <param name="Category">Recovery category requested by the caller.</param>
/// <param name="Interval">Optional original UTC interval submitted by the caller.</param>
/// <param name="GenesysJobId">
/// Optional existing Genesys job identifier to recover.
/// Supported only for <see cref="RecoveryCategory.ConversationsDetails"/>.
/// </param>
public sealed record CreateRecoveryRequestCommand(LobName Lob,
                                                  RecoveryCategory Category,
                                                  UtcInterval? Interval,
                                                  string? GenesysJobId) : IRequest<CreateRecoveryRequestResponse>;
