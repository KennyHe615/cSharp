using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Planning;
using Application.DTOs.Planning;
using Application.Enums;
using Application.Features.Shared;

using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.Planning;

/// <summary>
/// Unit tests for <see cref="IntervalPlanner"/>.
/// </summary>
public sealed class IntervalPlannerTests
{
    /// <summary>
    /// Verifies that unsupported analytics categories are rejected before planning.
    /// </summary>
    [Fact]
    public async Task PlanAsync_UnsupportedCategory_ShouldThrowIntervalPlanningException()
    {
        IntervalPlanner sut = BuildSutWithConstantHits(0, DateTimeOffset.UtcNow);

        UtcInterval interval = new UtcInterval(new DateTimeOffset(2026,
                                                                  3,
                                                                  1,
                                                                  0,
                                                                  0,
                                                                  0,
                                                                  TimeSpan.Zero),
                                               new DateTimeOffset(2026,
                                                                  3,
                                                                  1,
                                                                  1,
                                                                  0,
                                                                  0,
                                                                  TimeSpan.Zero));

        await Assert.ThrowsAsync<IntervalPlanningException>(() => sut.PlanAsync(SyncAnalyticsCategory
                                                                                           .ConversationsAggregates,
                                                                                    interval));
    }

    /// <summary>
    /// Verifies that intervals older than the Genesys historical limit are rejected.
    /// </summary>
    [Fact]
    public async Task PlanAsync_StartOlderThanHistoricalLimit_ShouldThrowIntervalPlanningException()
    {
        DateTimeOffset now = new DateTimeOffset(2026,
                                                3,
                                                17,
                                                12,
                                                0,
                                                0,
                                                TimeSpan.Zero);
        IntervalPlanner sut = BuildSutWithConstantHits(0, now);

        UtcInterval tooOld = new UtcInterval(new DateTimeOffset(2024,
                                                                1,
                                                                1,
                                                                0,
                                                                0,
                                                                0,
                                                                TimeSpan.Zero),
                                             new DateTimeOffset(2024,
                                                                1,
                                                                1,
                                                                1,
                                                                0,
                                                                0,
                                                                TimeSpan.Zero));

        await Assert.ThrowsAsync<IntervalPlanningException>(() => sut.PlanAsync(SyncAnalyticsCategory.UsersDetails,
                                                                                    tooOld));
    }

    /// <summary>
    /// Verifies that intervals above the hit threshold are split by binary search.
    /// </summary>
    [Fact]
    public async Task PlanAsync_WhenHitsExceedThreshold_ShouldSplitUsingBinarySearch()
    {
        DateTimeOffset now = new DateTimeOffset(2026,
                                                3,
                                                17,
                                                12,
                                                0,
                                                0,
                                                TimeSpan.Zero);
        IntervalPlanner sut = BuildSutWithDurationHits(duration => (int)Math.Round(duration.TotalMinutes), now, 100);

        UtcInterval interval = new UtcInterval(new DateTimeOffset(2026,
                                                                  3,
                                                                  17,
                                                                  0,
                                                                  0,
                                                                  0,
                                                                  TimeSpan.Zero),
                                               new DateTimeOffset(2026,
                                                                  3,
                                                                  17,
                                                                  3,
                                                                  0,
                                                                  0,
                                                                  TimeSpan.Zero));

        IReadOnlyList<PlannedIntervalDto> plan = await sut.PlanAsync(SyncAnalyticsCategory.UsersDetails, interval);

        Assert.Equal(2, plan.Count);

        Assert.Equal(99, (int)(plan[0].Interval.End - plan[0].Interval.Start).TotalMinutes);
        Assert.Equal(99, plan[0].TotalHits);

        Assert.Equal(81, (int)(plan[1].Interval.End - plan[1].Interval.Start).TotalMinutes);
        Assert.Equal(81, plan[1].TotalHits);
    }

    #region ========== *** Private Section *** ==========

    private static IntervalPlanner BuildSutWithConstantHits(int constantHits,
                                                            DateTimeOffset now,
                                                            int maxHitThreshold = 100_000)
    {
        IHitCountProvider provider = new ConstantHitCountProvider(constantHits);

        return BuildSut(provider, now, maxHitThreshold);
    }

    private static IntervalPlanner BuildSutWithDurationHits(Func<TimeSpan, int> hitCounter,
                                                            DateTimeOffset now,
                                                            int maxHitThreshold = 100_000)
    {
        IHitCountProvider provider = new DurationHitCountProvider(hitCounter);

        return BuildSut(provider, now, maxHitThreshold);
    }

    private static IntervalPlanner BuildSut(IHitCountProvider provider, DateTimeOffset now, int maxHitThreshold)
    {
        StubHitCountProviderFactory factory = new StubHitCountProviderFactory(provider);
        StubDateTimeProvider dateTimeProvider = new StubDateTimeProvider(now);

        GenesysOptions options = new GenesysOptions
                                 {
                                     OAuthBaseUrl = "https://oauth.example.com",
                                     ApiBaseUrl = "https://api.example.com",
                                     DefaultPageSize = 100,
                                     MaxHitThreshold = maxHitThreshold
                                 };

        return new IntervalPlanner(factory,
                                   dateTimeProvider,
                                   Options.Create(options),
                                   NullLogger<IntervalPlanner>.Instance);
    }

    private sealed class StubHitCountProviderFactory(IHitCountProvider provider) : IHitCountProviderFactory
    {
        public IHitCountProvider Create(SyncAnalyticsCategory category)
        {
            return provider;
        }
    }

    private sealed class ConstantHitCountProvider(int constantHits) : IHitCountProvider
    {
        public Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
        {
            return Task.FromResult(constantHits);
        }
    }

    private sealed class DurationHitCountProvider(Func<TimeSpan, int> hitCounter) : IHitCountProvider
    {
        public Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
        {
            return Task.FromResult(hitCounter(end - start));
        }
    }

    private sealed class StubDateTimeProvider(DateTimeOffset utcNow) : IDateTimeProvider
    {
        public TimeZoneInfo Eastern => TimeZoneInfo.Utc;

        public DateTime UtcNow => utcNow.UtcDateTime;

        public DateTime EstNow => utcNow.UtcDateTime;

        public DateTimeOffset UtcNowOffset => utcNow;

        public DateTimeOffset EstNowOffset => utcNow;

        public DateTimeOffset? ConvertToEst(DateTimeOffset? utc)
        {
            return utc;
        }

        public DateTimeOffset ConvertToEst(DateTimeOffset utc)
        {
            return utc;
        }
    }

    #endregion
}
