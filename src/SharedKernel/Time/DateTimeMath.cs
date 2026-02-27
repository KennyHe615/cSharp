namespace SharedKernel.Time;

public static class DateTimeMath
{
    public static long? CalculateDuration(DateTimeOffset startTime, DateTimeOffset? endTime) =>
        endTime.HasValue ? (long?)(endTime.Value - startTime).TotalSeconds : null;

    public static DateTimeOffset RoundToMinute(DateTimeOffset dateTime) =>
        RoundByUnit(dateTime, TimeSpan.TicksPerMinute);

    public static DateTimeOffset RoundToSeconds(DateTimeOffset dateTime) =>
        RoundByUnit(dateTime, TimeSpan.TicksPerSecond);

    #region ========== *** Private Methods *** ==========

    private static DateTimeOffset RoundByUnit(DateTimeOffset dateTime, long ticksPerUnit)
    {
        long ticks = dateTime.Ticks;
        long remainder = ticks % ticksPerUnit;

        long roundedTicks = remainder >= ticksPerUnit / 2 ? ticks + (ticksPerUnit - remainder) : ticks - remainder;

        return new DateTimeOffset(roundedTicks, dateTime.Offset);
    }

    #endregion
}
