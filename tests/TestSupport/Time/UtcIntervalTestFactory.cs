using SharedKernel.Time;


namespace tests.TestSupport.Time;

public static class UtcIntervalTestFactory
{
    public static UtcInterval Create()
    {
        return new UtcInterval(new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero),
                               new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero));
    }

    public static UtcInterval Create(DateTimeOffset start, DateTimeOffset end)
    {
        return new UtcInterval(start, end);
    }
}
