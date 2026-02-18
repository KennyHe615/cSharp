namespace Application.Features.Recovery;

public record CreateRecoveryRequestResponse(bool Success,
                                            string Message,
                                            object RequestedDetail);
