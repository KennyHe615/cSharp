using SharedKernel.Time;

using Xunit;


namespace tests.Unit.SharedKernel.Time;

public sealed class DateTimeMathTests
{
    #region CalculateDuration

    [Fact]
    public void CalculateDuration_ReturnsNull_WhenEndTimeIsNull()
    {
        DateTimeOffset start = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);

        long? actual = DateTimeMath.CalculateDuration(start, null);

        Assert.Null(actual);
    }

    [Fact]
    public void CalculateDuration_ReturnsSeconds_WhenEndTimeExists()
    {
        DateTimeOffset start = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);
        DateTimeOffset end = start.AddSeconds(125);

        long? actual = DateTimeMath.CalculateDuration(start, end);

        Assert.Equal(125L, actual);
    }

    [Fact]
    public void CalculateDuration_ReturnsNegativeSeconds_WhenEndBeforeStart()
    {
        DateTimeOffset start = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  10,
                                                  TimeSpan.Zero);
        DateTimeOffset end = start.AddSeconds(-3);

        long? actual = DateTimeMath.CalculateDuration(start, end);

        Assert.Equal(-3L, actual);
    }

    #endregion

    #region RoundToMinute

    [Fact]
    public void RoundToMinute_RoundsDown_WhenBelowHalfMinute()
    {
        DateTimeOffset input = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  29,
                                                  400,
                                                  TimeSpan.Zero);

        DateTimeOffset actual = DateTimeMath.RoundToMinute(input);

        Assert.Equal(new DateTimeOffset(2026,
                                        2,
                                        1,
                                        10,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     actual);
    }

    [Fact]
    public void RoundToMinute_RoundsUp_WhenAtOrAboveHalfMinute()
    {
        DateTimeOffset input = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  30,
                                                  0,
                                                  TimeSpan.Zero);

        DateTimeOffset actual = DateTimeMath.RoundToMinute(input);

        Assert.Equal(new DateTimeOffset(2026,
                                        2,
                                        1,
                                        10,
                                        1,
                                        0,
                                        TimeSpan.Zero),
                     actual);
    }

    #endregion

    #region RoundToSecond

    [Fact]
    public void RoundToSeconds_RoundsDown_WhenBelowHalfSecond()
    {
        DateTimeOffset input = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  0,
                                                  499,
                                                  TimeSpan.Zero);

        DateTimeOffset actual = DateTimeMath.RoundToSeconds(input);

        Assert.Equal(new DateTimeOffset(2026,
                                        2,
                                        1,
                                        10,
                                        0,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     actual);
    }

    [Fact]
    public void RoundToSeconds_RoundsUp_WhenAtOrAboveHalfSecond()
    {
        DateTimeOffset input = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  0,
                                                  500,
                                                  TimeSpan.Zero);

        DateTimeOffset actual = DateTimeMath.RoundToSeconds(input);

        Assert.Equal(new DateTimeOffset(2026,
                                        2,
                                        1,
                                        10,
                                        0,
                                        1,
                                        0,
                                        TimeSpan.Zero),
                     actual);
    }

    #endregion

    [Fact]
    public void RoundMethods_PreserveOffset()
    {
        TimeSpan offset = TimeSpan.FromHours(8);
        DateTimeOffset input = new DateTimeOffset(2026,
                                                  2,
                                                  1,
                                                  10,
                                                  0,
                                                  30,
                                                  600,
                                                  offset);

        DateTimeOffset minuteRounded = DateTimeMath.RoundToMinute(input);
        DateTimeOffset secondRounded = DateTimeMath.RoundToSeconds(input);

        Assert.Equal(offset, minuteRounded.Offset);
        Assert.Equal(offset, secondRounded.Offset);
    }
}
