using Application.Mediator;

using SharedKernel.Lobs;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Command to execute one UsersDetails incremental cycle for one LOB.
/// The handler joins an existing executable incremental request when available;
/// otherwise it reserves the next incremental window and creates executable sync work.
/// </summary>
/// <param name="Lob">LOB whose incremental cursor should be reserved.</param>
public sealed record RunUsersDetailsIncrementalCycleCommand(LobName Lob) : IRequest<long?>;
