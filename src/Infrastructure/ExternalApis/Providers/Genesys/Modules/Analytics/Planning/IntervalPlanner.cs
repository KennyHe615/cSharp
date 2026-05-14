using Application.Abstractions.Planning;
using Application.DTOs.Planning;
using Application.Enums;
using Application.Features.Shared;

using Infrastructure.ExternalApis.Providers.Genesys.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Time;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

/// <summary>
/// Genesys analytics interval planner implementation.
/// </summary>
/// <param name="hitCountProviderFactory">Factory that resolves category-specific hit-count providers.</param>
/// <param name="dateTimeProvider">Time provider used for historical-window validation.</param>
/// <param name="genesysOptions">Configured Genesys options.</param>
/// <param name="logger">Logger instance.</param>
public sealed class IntervalPlanner(IHitCountProviderFactory hitCountProviderFactory,
                                    IDateTimeProvider dateTimeProvider,
                                    IOptions<GenesysOptions> genesysOptions,
                                    ILogger<IntervalPlanner> logger) : IIntervalPlanner
{
    #region ========== *** Properties *** ==========

    private const int HistoricalDataLimitDays = GenesysOptions.HistoricalDataLimitDays;
    private const int MaxIntervalDays = GenesysOptions.MaxIntervalDays;

    private readonly IHitCountProviderFactory _hitCountProviderFactory =
            hitCountProviderFactory ?? throw new ArgumentNullException(nameof(hitCountProviderFactory));

    private readonly IDateTimeProvider _dateTimeProvider =
            dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

    private readonly GenesysOptions _genesysOptions =
            genesysOptions.Value ?? throw new ArgumentNullException(nameof(genesysOptions));

    private readonly ILogger<IntervalPlanner> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    #endregion

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlannedIntervalDto>> PlanAsync(SyncAnalyticsCategory category,
                                                                   UtcInterval interval,
                                                                   CancellationToken ct = default)
    {
        ValidateInputs(category, interval);

        IHitCountProvider provider = _hitCountProviderFactory.Create(category);

        return await BuildPlanAsync(interval, provider, ct)
                      .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private void ValidateInputs(SyncAnalyticsCategory category, UtcInterval interval)
    {
        if (category != SyncAnalyticsCategory.UsersDetails && category != SyncAnalyticsCategory.ConversationsDetails)
        {
            throw new
                    IntervalPlanningException($"Category '{category}' is not supported by interval planner. Supported categories: UsersDetails, ConversationsDetails.");
        }

        // Genesys historical constraint: start cannot be older than configured limit.
        DateTimeOffset now = _dateTimeProvider.UtcNowOffset;
        DateTimeOffset earliestAllowedStart = now.AddDays(-HistoricalDataLimitDays);

        if (interval.Start < earliestAllowedStart)
        {
            throw new
                    IntervalPlanningException($"Interval start ({interval.Start:O}) exceeds Genesys historical limit ({HistoricalDataLimitDays} days). "
                                              + $"Earliest allowed start: {earliestAllowedStart:O}.");
        }
    }

    private async Task<IReadOnlyList<PlannedIntervalDto>> BuildPlanAsync(UtcInterval interval,
                                                                         IHitCountProvider provider,
                                                                         CancellationToken ct)
    {
        List<PlannedIntervalDto> plannedIntervals = [];
        DateTimeOffset currentStart = interval.Start;

        // Build contiguous slices from interval.Start to interval.End.
        while (currentStart < interval.End)
        {
            ct.ThrowIfCancellationRequested();

            (DateTimeOffset optimalEnd, int hits) = await FindOptimalEndAsync(currentStart,
                                                                              interval.End,
                                                                              provider,
                                                                              ct)
                                                           .ConfigureAwait(false);

            PlannedIntervalDto planned = new PlannedIntervalDto(new UtcInterval(currentStart, optimalEnd), hits);
            plannedIntervals.Add(planned);

            _logger.LogDebug("Planned interval {Interval} with {Hits} hits and {Pages} pages.",
                             planned.Interval,
                             planned.TotalHits,
                             planned.TotalPages);

            currentStart = optimalEnd;
        }

        return plannedIntervals;
    }

    private async Task<(DateTimeOffset End, int Hits)> FindOptimalEndAsync(DateTimeOffset start,
                                                                           DateTimeOffset maxEnd,
                                                                           IHitCountProvider provider,
                                                                           CancellationToken ct)
    {
        // Never exceed Genesys max interval length for a single query.
        DateTimeOffset upperBound = Min(start.AddDays(MaxIntervalDays), maxEnd);

        int fullHits = await provider.GetHitCountAsync(start, upperBound, ct)
                                     .ConfigureAwait(false);

        // Fast path: full allowed range is already under threshold.
        if (fullHits < _genesysOptions.MaxHitThreshold) return (upperBound, fullHits);

        // Binary search for the largest minute-window [start, start+N] under hit threshold.
        // Assumption: hits are monotonic with respect to end time.
        int totalMinutes = Math.Max(1, (int)Math.Floor((upperBound - start).TotalMinutes));
        int low = 1;
        int high = totalMinutes;

        int bestMinutes = 1;
        int bestHits = int.MaxValue;
        Dictionary<int, int> hitCache = new Dictionary<int, int>();

        while (low <= high)
        {
            ct.ThrowIfCancellationRequested();

            int mid = low + (high - low) / 2;
            int hits = await GetHitsForMinutesAsync(hitCache,
                                                    start,
                                                    mid,
                                                    provider,
                                                    ct)
                              .ConfigureAwait(false);

            if (hits < _genesysOptions.MaxHitThreshold)
            {
                bestMinutes = mid;
                bestHits = hits;
                low = mid + 1;// try a larger interval
            }
            else
            {
                high = mid - 1;// interval too large, shrink
            }
        }

        if (bestHits != int.MaxValue) return (start.AddMinutes(bestMinutes), bestHits);

        // Even 1 minute exceeds threshold: accept minimal slice and continue.
        bestHits = await GetHitsForMinutesAsync(hitCache,
                                                start,
                                                1,
                                                provider,
                                                ct)
                          .ConfigureAwait(false);

        return (start.AddMinutes(1), bestHits);
    }

    private static async Task<int> GetHitsForMinutesAsync(Dictionary<int, int> hitCache,
                                                          DateTimeOffset start,
                                                          int minutes,
                                                          IHitCountProvider provider,
                                                          CancellationToken ct)
    {
        // Cache avoids duplicate API calls for repeated midpoints during binary search.
        if (hitCache.TryGetValue(minutes, out int cached)) return cached;

        DateTimeOffset end = start.AddMinutes(minutes);
        int hits = await provider.GetHitCountAsync(start, end, ct)
                                 .ConfigureAwait(false);
        hitCache[minutes] = hits;

        return hits;
    }

    private static DateTimeOffset Min(DateTimeOffset left, DateTimeOffset right)
    {
        return left <= right ? left : right;
    }

    #endregion
}
