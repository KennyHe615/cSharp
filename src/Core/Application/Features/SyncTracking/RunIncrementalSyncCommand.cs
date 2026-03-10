using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking;

/// <summary>
/// Command to run an incremental sync for a single request scope.
/// </summary>
/// <param name="Category">Sync category to execute.</param>
/// <param name="Interval">
/// Time window identifier for analytics sync. Can be <c>null</c> for references sync.
/// </param>
/// <param name="PageNumber">
/// Optional page number when a category supports paged incremental processing.
/// </param>
public sealed record RunIncrementalSyncCommand(SyncCategory Category,
                                               string? Interval,
                                               int? PageNumber) : IRequest<long>;
