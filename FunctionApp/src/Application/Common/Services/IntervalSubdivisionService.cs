using Application.Common.Abstractions.Factories;
using Application.Common.Abstractions.Providers;
using Application.Common.Abstractions.Services;
using Application.Common.Enums;
using Application.Common.Exceptions;
using Application.Common.Factories;
using Application.Common.Models;

using Configuration.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Constants;
using Shared.Time;


namespace Application.Common.Services;

/// <summary>
/// Service for subdividing time intervals into smaller chunks based on hit count thresholds.
/// </summary>
public sealed class IntervalSubdivisionService(IHitCountProviderFactory hitCountProviderFactory,
                                               IOptions<IntervalSubdivisionOptions> options,
                                               IDateTimeProvider dateTimeProvider,
                                               ILogger<IntervalSubdivisionService> logger) : IIntervalSubdivisionService
{
    private readonly IntervalSubdivisionOptions _options = options.Value;
    private const int HistoricalDataLimitDays = GenesysConstants.HistoricalDataLimitDays;
    private const int MaxIntervalDays = GenesysConstants.MaxIntervalDays;
    private const int MaxHitThreshold = GenesysConstants.MaxHitThreshold;

    /// <inheritdoc />
    public async Task<List<IntervalWithPages>> SubdivideAsync(Interval interval,
                                                              SyncCategory category,
                                                              CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(interval);
        ArgumentNullException.ThrowIfNull(category);

        if (!IntervalFactory.IsValid(interval))
        {
            throw new IntervalSubdivisionException($"Interval{interval} validation failed");
        }

        DateTimeOffset now = dateTimeProvider.UtcNowOffset;
        DateTimeOffset genesysHistoricalLimit = now.AddDays(-HistoricalDataLimitDays);

        if (interval.Start < genesysHistoricalLimit)
        {
            throw new IntervalSubdivisionException(
                $"Interval start time ({interval.Start:O}) exceeds Genesys historical data limit. " +
                $"Data must be within {HistoricalDataLimitDays} days from now ({now:O}). " +
                $"Earliest allowed start: {genesysHistoricalLimit:O}");
        }

        IHitCountProvider hitCountProvider = hitCountProviderFactory.Create(category);

        return await SubdivideInternalAsync(interval.Start, interval.End, hitCountProvider, ct);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Internal method to perform the interval subdivision.
    /// </summary>
    /// <param name="start">The start time of the interval.</param>
    /// <param name="end">The end time of the interval.</param>
    /// <param name="hitCountProvider">The provider for retrieving hit counts.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of subdivided intervals with page information.</returns>
    private async Task<List<IntervalWithPages>> SubdivideInternalAsync(DateTimeOffset start,
                                                                       DateTimeOffset end,
                                                                       IHitCountProvider hitCountProvider,
                                                                       CancellationToken ct)
    {
        List<IntervalWithPages> results = [];

        DateTimeOffset normalizedStart = DateTimeResolver.NormalizeToMinute(start);
        DateTimeOffset normalizedEnd = DateTimeResolver.NormalizeToMinute(end);

        DateTimeOffset currentStart = normalizedStart;

        while (currentStart < normalizedEnd)
        {
            ct.ThrowIfCancellationRequested();

            (DateTimeOffset optimalEnd, long hitCount) =
                await FindOptimalEndTimeAsync(currentStart, normalizedEnd, hitCountProvider, ct);

            Interval subInterval = IntervalFactory.FromDateTimeOffset(currentStart, optimalEnd);
            IntervalWithPages intervalWithPages = IntervalWithPages.Create(subInterval, hitCount);

            results.Add(intervalWithPages);

            logger.LogDebug("Created sub-interval - {Interval}, Hits: {Hits}, Pages: {Pages}",
                            subInterval,
                            hitCount,
                            intervalWithPages.TotalPages);

            currentStart = optimalEnd;
        }

        return results;
    }

    /// <summary>
    /// Finds the optimal end time for a sub-interval by querying hit counts and reducing interval size if needed.
    /// </summary>
    /// <param name="currentStart">The start time of the current sub-interval.</param>
    /// <param name="maxEnd">The maximum allowed end time.</param>
    /// <param name="hitCountProvider">The provider for retrieving hit counts.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A tuple containing the optimal end time and the hit count for that interval.</returns>
    private async Task<(DateTimeOffset EndTime, long HitCount)> FindOptimalEndTimeAsync(
        DateTimeOffset currentStart,
        DateTimeOffset maxEnd,
        IHitCountProvider hitCountProvider,
        CancellationToken ct)
    {
        DateTimeOffset nextBoundary = currentStart.AddDays(MaxIntervalDays);
        DateTimeOffset candidateEnd = nextBoundary < maxEnd ? nextBoundary : maxEnd;

        long hitCount = await hitCountProvider.GetHitCountAsync(currentStart, candidateEnd, ct);

        if (hitCount < MaxHitThreshold) return (candidateEnd, hitCount);

        while (hitCount >= MaxHitThreshold)
        {
            ct.ThrowIfCancellationRequested();

            TimeSpan currentDuration = candidateEnd - currentStart;
            double totalMinutes = currentDuration.TotalMinutes;

            if (totalMinutes <= _options.MinIntervalMinutes)
            {
                logger.LogWarning(
                    "Reached minimum interval ({Minutes} min) but hits ({Hits}) still exceed threshold ({Threshold}). Accepting interval.",
                    totalMinutes,
                    hitCount,
                    MaxHitThreshold);

                break;
            }

            int reductionMinutes = hitCount >= _options.HighHitThreshold
                ? Math.Max(_options.MinIntervalMinutes, (int)Math.Round(totalMinutes / 2))
                : Math.Max(_options.MinIntervalMinutes, (int)Math.Round(totalMinutes - _options.AdjustmentMinutes));

            candidateEnd = currentStart.AddMinutes(reductionMinutes);

            hitCount = await hitCountProvider.GetHitCountAsync(currentStart, candidateEnd, ct);
        }

        return (candidateEnd, hitCount);
    }

    #endregion
}
