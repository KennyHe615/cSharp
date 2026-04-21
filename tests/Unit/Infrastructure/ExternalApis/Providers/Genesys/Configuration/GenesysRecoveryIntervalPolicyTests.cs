using Infrastructure.ExternalApis.Providers.Genesys.Configuration;

using Microsoft.Extensions.Options;

using tests.TestSupport.Time;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Providers.Genesys.Configuration;

public sealed class GenesysRecoveryIntervalPolicyTests
{
    [Fact]
    public void HistoricalDataLimitDays_ReturnsGenesysHistoricalLimit()
    {
        GenesysRecoveryIntervalPolicy sut = CreateSut();

        Assert.Equal(GenesysOptions.HistoricalDataLimitDays, sut.HistoricalDataLimitDays);
    }

    [Fact]
    public void FutureSkewDays_ReturnsConfiguredRecoveryFutureSkewDays()
    {
        GenesysRecoveryIntervalPolicy sut = CreateSut(recoveryFutureSkewDays: 3);

        Assert.Equal(3, sut.FutureSkewDays);
    }

    [Fact]
    public void IsStartWithinRetention_WhenStartEqualsEarliestAllowed_ReturnsTrue()
    {
        GenesysRecoveryIntervalPolicy sut = CreateSut();
        DateTimeOffset start = DateTimeProviderTestFactory.FixedNow.AddDays(-GenesysOptions.HistoricalDataLimitDays);

        bool result = sut.IsStartWithinRetention(start);

        Assert.True(result);
    }

    [Fact]
    public void IsStartWithinRetention_WhenStartOlderThanEarliestAllowed_ReturnsFalse()
    {
        GenesysRecoveryIntervalPolicy sut = CreateSut();
        DateTimeOffset start = DateTimeProviderTestFactory.FixedNow.AddDays(-GenesysOptions.HistoricalDataLimitDays)
                                                          .AddTicks(-1);

        bool result = sut.IsStartWithinRetention(start);

        Assert.False(result);
    }

    [Fact]
    public void IsEndWithinFutureSkew_WhenEndEqualsLatestAllowed_ReturnsTrue()
    {
        GenesysRecoveryIntervalPolicy sut = CreateSut(recoveryFutureSkewDays: 1);
        DateTimeOffset end = DateTimeProviderTestFactory.FixedNow.AddDays(1);

        bool result = sut.IsEndWithinFutureSkew(end);

        Assert.True(result);
    }

    [Fact]
    public void IsEndWithinFutureSkew_WhenEndExceedsLatestAllowed_ReturnsFalse()
    {
        GenesysRecoveryIntervalPolicy sut = CreateSut(recoveryFutureSkewDays: 1);
        DateTimeOffset end = DateTimeProviderTestFactory.FixedNow.AddDays(1)
                                                        .AddTicks(1);

        bool result = sut.IsEndWithinFutureSkew(end);

        Assert.False(result);
    }

    #region ========== *** Private Section *** ==========

    private static GenesysRecoveryIntervalPolicy CreateSut(int recoveryFutureSkewDays = 1)
    {
        GenesysOptions options = new GenesysOptions
                                 {
                                     OAuthBaseUrl = "https://login.example.com",
                                     ApiBaseUrl = "https://api.example.com",
                                     RecoveryFutureSkewDays = recoveryFutureSkewDays
                                 };

        return new GenesysRecoveryIntervalPolicy(Options.Create(options),
                                                 DateTimeProviderTestFactory.Create()
                                                                            .Object);
    }

    #endregion
}
