using Application.Enums;


namespace Application.Features.Analytics.Shared;

/// <summary>
/// Shared execution request for analytics categories that sync by interval and page number.
/// </summary>
/// <param name="RunId">Physical sync run identifier.</param>
/// <param name="Category">Analytics category being executed.</param>
/// <param name="Mode">Sync mode for the executable request.</param>
/// <param name="Interval">Normalized UTC interval used for the external analytics request.</param>
/// <param name="RequestedPageNumber">Optional one-based page number for single-page recovery.</param>
/// <param name="ResolvePagesAsync">Callback that resolves the page numbers to seed for this execution.</param>
/// <param name="ProcessPageAsync">Callback that fetches, normalizes, and persists one claimed page.</param>
public sealed record AnalyticsPageSyncRequest(long RunId,
                                              SyncAnalyticsCategory Category,
                                              SyncMode Mode,
                                              string Interval,
                                              int? RequestedPageNumber,
                                              Func<CancellationToken, Task<IReadOnlyCollection<int>>> ResolvePagesAsync,
                                              Func<int, CancellationToken, Task> ProcessPageAsync);
