using Application.Common.Enums;
using Application.Common.Mediator;
using Application.Contracts.Recovery;


namespace Application.Features.Recovery;

public record CreateRecoveryRequestCommand(RecoveryLob Lob,
                                           SyncCategory? Category,
                                           string? Interval,
                                           string? JobId) : IRequest<CreateRecoveryRequestResponse>;
