using Application.Common.Abstractions.Context;
using Application.Common.Abstractions.Services;
using Application.Common.Enums;
using Application.Common.Factories;
using Application.Common.Models;
using Application.Normalizers.UserDetails;
using Application.UserDetails;

using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Infrastructure.Services.UserDetails;

public class UserDetailsIncrementalSyncService(IUserDetailsClient client,
                                               IIntervalSubdivisionService subdivisionService,
                                               IUserDetailsNormalizer normalizer,
                                               IUserDetailsRepository repository,
                                               ILobContext lobContext,
                                               ILogger<UserDetailsIncrementalSyncService> logger)
    : UserDetailsSyncServiceBase(client, normalizer, repository, lobContext, logger), IUserDetailsSyncService
{
    private readonly IIntervalSubdivisionService _subdivisionService =
        subdivisionService ?? throw new ArgumentNullException(nameof(subdivisionService));

    public async Task SyncUserDetailsIncrementalAsync(CancellationToken ct)
    {
        string lobName = LobContext.LobName;
        const string category = nameof(SyncCategory.UserDetailsIncremental);

        // TODO: Getting interval from database
        const string intervalString = "2026-01-01T00:00Z/2026-01-11T05:00Z";
        Logger.LogInformation(CommonConstants.LobCategoryLogPrefix + "Starting sync for interval **{Interval}**",
                              lobName,
                              category,
                              intervalString);

        List<IntervalWithPages> intervalsWithPages;
        try
        {
            intervalsWithPages = await _subdivisionService.SubdivideAsync(
                IntervalFactory.FromString(intervalString),
                SyncCategory.UserDetailsIncremental,
                ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                            CommonConstants.LobCategoryLogPrefix + "Failed to subdivide interval **{Interval}**",
                            lobName,
                            category,
                            intervalString);

            throw;
        }

        int totalIntervals = intervalsWithPages.Count;
        int totalPages = intervalsWithPages.Sum(x => x.TotalPages);
        int processedIntervals = 0;
        int processedPages = 0;

        foreach (IntervalWithPages intervalWithPages in intervalsWithPages)
        {
            if (intervalWithPages.TotalPages == 0)
            {
                Logger.LogWarning(
                    CommonConstants.LobCategoryLogPrefix + "Skipping interval **{Interval}** - no data (0 pages)",
                    lobName,
                    category,
                    intervalWithPages.Interval);

                continue;
            }

            Logger.LogInformation(
                CommonConstants.LobCategoryLogPrefix +
                "Processing interval **{Interval}** ({CurrentInterval}/{TotalIntervals}) - {Pages} pages",
                lobName,
                category,
                intervalWithPages.Interval,
                ++processedIntervals,
                totalIntervals,
                intervalWithPages.TotalPages);

            for (int pageNumber = 1; pageNumber <= intervalWithPages.TotalPages; pageNumber++)
            {
                try
                {
                    await SyncIntervalPageAsync(intervalWithPages.Interval, pageNumber, ct);
                    processedPages++;

                    Logger.LogDebug(
                        CommonConstants.LobCategoryLogPrefix +
                        "Interval **{Interval}** page {Page}/{TotalPages} completed ({Progress}%)",
                        lobName,
                        category,
                        intervalWithPages.Interval,
                        pageNumber,
                        intervalWithPages.TotalPages,
                        (int)((double)processedPages / totalPages * 100));
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex,
                                    CommonConstants.LobCategoryLogPrefix +
                                    "Failed to sync interval **{Interval}** page {Page}/{TotalPages}",
                                    lobName,
                                    category,
                                    intervalWithPages.Interval,
                                    pageNumber,
                                    intervalWithPages.TotalPages);

                    throw;
                }
            }
        }

        Logger.LogInformation(
            CommonConstants.LobCategoryLogPrefix + "Completed sync for {IntervalCount} interval(s), {PageCount} pages",
            lobName,
            category,
            totalIntervals,
            totalPages);
    }

    public async Task SyncUserDetailsRecoveryAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
    }
}
