using Application.Enums;
using Application.Mediator;


namespace Application.Features.Analytics.Recovery;

/// <summary>
/// Command to run a recovery sync for a single claimed request scope.
/// </summary>
/// <param name="RequestId">Internal id of the already-claimed recovery sync request.</param>
/// <param name="Category">Recovery target category.</param>
/// <param name="Interval">
/// Optional interval selector for interval-based recovery.
/// Must not be provided together with <paramref name="GenesysJobId"/>.
/// </param>
/// <param name="PageNumber">
/// Optional page selector for paged recovery workflows.
/// </param>
/// <param name="GenesysJobId">
/// Optional external Genesys job identifier for job-based recovery.
/// Supported only for <see cref="SyncAnalyticsCategory.ConversationsDetails"/>.
/// Must not be provided together with <paramref name="Interval"/>.
/// </param>
public sealed record RunAnalyticsRecoverySyncCommand(long RequestId,
                                                     SyncAnalyticsCategory Category,
                                                     string? Interval,
                                                     int? PageNumber,
                                                     string? GenesysJobId) : IRequest<long>;
