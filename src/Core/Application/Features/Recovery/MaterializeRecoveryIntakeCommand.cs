using Application.Enums;
using Application.Mediator;


namespace Application.Features.Recovery;

/// <summary>
/// Command to materialize one pending recovery intake request into executable sync_request rows.
/// </summary>
/// <param name="Category">
/// Optional analytics category filter. When null, the oldest pending intake request across all categories is selected.
/// </param>
public sealed record MaterializeRecoveryIntakeCommand(SyncAnalyticsCategory? Category) : IRequest<bool>;
