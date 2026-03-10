namespace Application.Contracts.InternalApis.Recovery;

public sealed record CreateRecoveryRequestResponse(bool Success,
                                                   string Message,
                                                   object Data);
