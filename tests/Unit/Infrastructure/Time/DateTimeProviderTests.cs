using System.Runtime.InteropServices;

using Infrastructure.Time;

using Xunit;


namespace tests.Unit.Infrastructure.Time;

public sealed class DateTimeProviderTests
{
    [Fact]
    public void Eastern_ReturnsExpectedTimeZone_ForCurrentOs()
    {
        DateTimeProvider sut = new DateTimeProvider();

        const string windowsId = "Eastern Standard Time";
        const string ianaId = "America/New_York";
        string expectedId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? windowsId : ianaId;

        Assert.Equal(expectedId, sut.Eastern.Id);
    }

    [Fact]
    public void UtcNow_IsUtcKind()
    {
        DateTimeProvider sut = new DateTimeProvider();

        DateTime actual = sut.UtcNow;

        Assert.Equal(DateTimeKind.Utc, actual.Kind);
    }

    [Fact]
    public void UtcNowOffset_HasZeroOffset()
    {
        DateTimeProvider sut = new DateTimeProvider();

        DateTimeOffset actual = sut.UtcNowOffset;

        Assert.Equal(TimeSpan.Zero, actual.Offset);
    }

    [Fact]
    public void EstNow_RepresentsCurrentTimeInEastern()
    {
        DateTimeProvider sut = new DateTimeProvider();

        DateTime utcBefore = DateTime.UtcNow;
        DateTime estNow = sut.EstNow;
        DateTime utcAfter = DateTime.UtcNow;

        DateTime estAsUtc = TimeZoneInfo.ConvertTimeToUtc(estNow, sut.Eastern);

        Assert.InRange(estAsUtc, utcBefore.AddSeconds(-1), utcAfter.AddSeconds(1));
    }

    [Fact]
    public void EstNowOffset_UsesEasternOffset()
    {
        DateTimeProvider sut = new DateTimeProvider();

        DateTimeOffset actual = sut.EstNowOffset;
        TimeSpan expectedOffset = sut.Eastern.GetUtcOffset(actual.UtcDateTime);

        Assert.Equal(expectedOffset, actual.Offset);
    }

    #region ConvertToEst

    [Fact]
    public void ConvertToEst_ReturnsNull_WhenInputIsNull()
    {
        DateTimeProvider sut = new DateTimeProvider();

        DateTimeOffset? actual = sut.ConvertToEst(null);

        Assert.Null(actual);
    }

    [Fact]
    public void ConvertToEst_Nullable_MatchesTimeZoneConversion()
    {
        DateTimeProvider sut = new DateTimeProvider();
        DateTimeOffset utc = new DateTimeOffset(2026,
                                                1,
                                                15,
                                                18,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        DateTimeOffset? actual = sut.ConvertToEst((DateTimeOffset?)utc);
        DateTimeOffset expected = TimeZoneInfo.ConvertTime(utc, sut.Eastern);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertToEst_NonNullable_MatchesTimeZoneConversion()
    {
        DateTimeProvider sut = new DateTimeProvider();
        DateTimeOffset utc = new DateTimeOffset(2026,
                                                7,
                                                15,
                                                18,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        DateTimeOffset actual = sut.ConvertToEst(utc);
        DateTimeOffset expected = TimeZoneInfo.ConvertTime(utc, sut.Eastern);

        Assert.Equal(expected, actual);
    }

    #endregion
}
