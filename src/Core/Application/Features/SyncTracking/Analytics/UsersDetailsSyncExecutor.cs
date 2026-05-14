using Application.Abstractions.External;
using Application.Abstractions.Normalization;
using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Analytics;
using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.DTOs.Planning;
using Application.DTOs.UsersDetails;
using Application.Enums;

using SharedKernel.Time;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Executes UsersDetails analytics sync requests.
/// Category-specific behavior is limited to page resolution, external fetch, normalization, and persistence.
/// </summary>
public sealed class UsersDetailsSyncExecutor(IAnalyticsUsersDetailsClient usersDetailsClient,
                                             IUsersDetailsNormalizer usersDetailsNormalizer,
                                             IUserDetailsRepository userDetailsRepository,
                                             IAnalyticsPageSyncCoordinator pageSyncCoordinator) : IAnalyticsSyncExecutor
{
    /// <inheritdoc />
    public SyncAnalyticsCategory Category => SyncAnalyticsCategory.UsersDetails;

    /// <inheritdoc />
    public async Task<SyncExecutionResult> ExecuteAsync(long runId,
                                                        SyncMode mode,
                                                        string? interval,
                                                        int? pageNumber,
                                                        string? genesysJobId,
                                                        CancellationToken ct)
    {
        if (mode != SyncMode.Incremental && mode != SyncMode.Recovery)
        {
            throw new NotSupportedException("UsersDetails accepts Incremental or Recovery mode only.");
        }

        if (!string.IsNullOrWhiteSpace(genesysJobId))
        {
            throw new NotSupportedException("UsersDetails does not support GenesysJobId execution.");
        }

        string normalizedInterval = UtcInterval.Normalize(interval ?? string.Empty);

        AnalyticsPageSyncRequest request = new AnalyticsPageSyncRequest(runId,
                                                                        Category,
                                                                        mode,
                                                                        normalizedInterval,
                                                                        pageNumber,
                                                                        token =>
                                                                                ResolvePagesAsync(normalizedInterval,
                                                                                    pageNumber,
                                                                                    token),
                                                                        (claimedPageNumber, token) =>
                                                                                ProcessPageAsync(normalizedInterval,
                                                                                    claimedPageNumber,
                                                                                    token));

        return await pageSyncCoordinator.ExecuteAsync(request, ct)
                                        .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private async Task<IReadOnlyCollection<int>> ResolvePagesAsync(string interval,
                                                                   int? requestedPageNumber,
                                                                   CancellationToken ct)
    {
        if (requestedPageNumber.HasValue)
        {
            if (requestedPageNumber.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requestedPageNumber),
                                                      requestedPageNumber.Value,
                                                      "Page number must be greater than or equal to 1.");
            }

            return [requestedPageNumber.Value];
        }

        UtcInterval parsedInterval = UtcInterval.Parse(interval);

        int totalHits = await usersDetailsClient.GetHitCountAsync(parsedInterval.Start, parsedInterval.End, ct)
                                                .ConfigureAwait(false);

        int totalPages = new PlannedIntervalDto(parsedInterval, totalHits).TotalPages;

        return Enumerable.Range(1, totalPages)
                         .ToArray();
    }

    private async Task ProcessPageAsync(string interval, int pageNumber, CancellationToken ct)
    {
        UsersDetailsRawContract response = await usersDetailsClient.GetUsersDetailsAsync(interval, pageNumber, ct: ct)
                                                                   .ConfigureAwait(false);

        (IReadOnlyCollection<PrimaryPresenceDto> primaryPresenceDtos,
         IReadOnlyCollection<RoutingStatusDto> routingStatusDtos) =
                usersDetailsNormalizer.NormalizeUsersDetails(response);

        await userDetailsRepository.UpsertUserDetailsAsync(primaryPresenceDtos, routingStatusDtos, ct)
                                   .ConfigureAwait(false);
    }

    #endregion
}
