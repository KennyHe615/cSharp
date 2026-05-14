using Application.Mediator;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Command to execute one UsersDetails recovery cycle.
/// The handler atomically starts one eligible UsersDetails recovery request and dispatches it.
/// </summary>
public sealed record RunUsersDetailsRecoveryCycleCommand : IRequest<long?>;
