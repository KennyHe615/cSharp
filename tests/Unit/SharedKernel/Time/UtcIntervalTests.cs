using SharedKernel.Time;

using Xunit;


namespace tests.Unit.SharedKernel.Time;

public sealed class UtcIntervalTests
{
    #region ========== *** Constructor *** ==========

    [Fact]
    public void Constructor_WithValidUtcValues_CreatesInterval()
    {
        DateTimeOffset start = new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);
        DateTimeOffset end = new DateTimeOffset(2025,
                                                1,
                                                1,
                                                1,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        UtcInterval interval = new UtcInterval(start, end);

        Assert.Equal(start, interval.Start);
        Assert.Equal(end, interval.End);
    }

    [Fact]
    public void Constructor_WithNonUtcStart_Throws()
    {
        DateTimeOffset start = new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.FromHours(1));
        DateTimeOffset end = new DateTimeOffset(2025,
                                                1,
                                                1,
                                                1,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => new UtcInterval(start, end));
        Assert.Contains("Start must be UTC", ex.Message);
    }

    [Fact]
    public void Constructor_WithNonUtcEnd_Throws()
    {
        DateTimeOffset start = new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);
        DateTimeOffset end = new DateTimeOffset(2025,
                                                1,
                                                1,
                                                1,
                                                0,
                                                0,
                                                TimeSpan.FromHours(1));

        ArgumentException ex = Assert.Throws<ArgumentException>(() => new UtcInterval(start, end));
        Assert.Contains("End must be UTC", ex.Message);
    }

    [Fact]
    public void Constructor_WithStartEqualEnd_Throws()
    {
        DateTimeOffset start = new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => new UtcInterval(start, start));
        Assert.Contains("Start must be before End", ex.Message);
    }

    [Fact]
    public void Constructor_WithStartAfterEnd_Throws()
    {
        DateTimeOffset start = new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  2,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero);
        DateTimeOffset end = new DateTimeOffset(2025,
                                                1,
                                                1,
                                                1,
                                                0,
                                                0,
                                                TimeSpan.Zero);

        ArgumentException ex = Assert.Throws<ArgumentException>(() => new UtcInterval(start, end));
        Assert.Contains("Start must be before End", ex.Message);
    }

    #endregion

    #region ========== *** Parse *** ==========

    [Fact]
    public void Parse_WithValidMinutePrecision_ReturnsInterval()
    {
        UtcInterval interval = UtcInterval.Parse("2025-01-01T00:00Z/2025-01-01T01:00Z");

        Assert.Equal(new DateTimeOffset(2025,
                                        1,
                                        1,
                                        0,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     interval.Start);
        Assert.Equal(new DateTimeOffset(2025,
                                        1,
                                        1,
                                        1,
                                        0,
                                        0,
                                        TimeSpan.Zero),
                     interval.End);
    }

    [Fact]
    public void Parse_WithValidSecondPrecision_ReturnsInterval()
    {
        UtcInterval interval = UtcInterval.Parse("2025-01-01T00:00:30Z/2025-01-01T01:00:45Z");

        Assert.Equal(new DateTimeOffset(2025,
                                        1,
                                        1,
                                        0,
                                        0,
                                        30,
                                        TimeSpan.Zero),
                     interval.Start);
        Assert.Equal(new DateTimeOffset(2025,
                                        1,
                                        1,
                                        1,
                                        0,
                                        45,
                                        TimeSpan.Zero),
                     interval.End);
    }

    [Fact]
    public void Parse_WithInvalidFormat_ThrowsFormatException()
    {
        FormatException ex = Assert.Throws<FormatException>(() => UtcInterval.Parse("invalid"));
        Assert.Contains("Invalid interval format", ex.Message);
    }

    #endregion

    #region ========== *** TryParse *** ==========

    [Fact]
    public void TryParse_WithNullOrWhiteSpace_ReturnsFalse()
    {
        Assert.False(UtcInterval.TryParse(null, out _));
        Assert.False(UtcInterval.TryParse("", out _));
        Assert.False(UtcInterval.TryParse("   ", out _));
    }

    [Fact]
    public void TryParse_WithMissingSlash_ReturnsFalse()
    {
        bool ok = UtcInterval.TryParse("2025-01-01T00:00Z", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryParse_WithExtraParts_ReturnsFalse()
    {
        bool ok = UtcInterval.TryParse("a/b/c", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryParse_WithNonUtcOffset_ReturnsFalse()
    {
        bool ok = UtcInterval.TryParse("2025-01-01T00:00+01:00/2025-01-01T01:00+01:00", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryParse_WithStartNotBeforeEnd_ReturnsFalse()
    {
        bool ok = UtcInterval.TryParse("2025-01-01T01:00Z/2025-01-01T01:00Z", out _);

        Assert.False(ok);
    }

    #endregion

    #region ========== *** ToString *** ==========

    [Fact]
    public void ToString_FormatsAsMinutePrecisionUtc()
    {
        UtcInterval interval = new UtcInterval(new DateTimeOffset(2025,
                                                                  1,
                                                                  1,
                                                                  0,
                                                                  0,
                                                                  30,
                                                                  TimeSpan.Zero),
                                               new DateTimeOffset(2025,
                                                                  1,
                                                                  1,
                                                                  1,
                                                                  0,
                                                                  45,
                                                                  TimeSpan.Zero));

        string formatted = interval.ToString();

        Assert.Equal("2025-01-01T00:00Z/2025-01-01T01:00Z", formatted);
    }

    #endregion

    #region ========== *** Normalize *** ==========

    [Fact]
    public void Normalize_WithValidSecondPrecision_ReturnsCanonicalMinutePrecision()
    {
        string normalized = UtcInterval.Normalize("2025-01-01T00:00:30Z/2025-01-01T01:00:45Z");

        Assert.Equal("2025-01-01T00:00Z/2025-01-01T01:00Z", normalized);
    }

    [Fact]
    public void Normalize_WithInvalidFormat_ThrowsFormatException()
    {
        FormatException ex = Assert.Throws<FormatException>(() => UtcInterval.Normalize("invalid"));

        Assert.Contains("Invalid interval format", ex.Message);
    }

    #endregion
}
