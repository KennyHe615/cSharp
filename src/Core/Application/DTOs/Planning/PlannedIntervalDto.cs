using SharedKernel.Time;


namespace Application.DTOs.Planning;

/// <summary>
/// Planner output for one analytics sub-interval.
/// </summary>
/// <param name="Interval">Normalized UTC interval slice.</param>
/// <param name="TotalHits">Total hits for this slice.</param>
public sealed record PlannedIntervalDto(UtcInterval Interval,
                                        int TotalHits)
{
    private static int _configuredPageSize = 100;

    public static void ConfigurePageSize(int pageSize)
    {
        if (pageSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
        }

        _configuredPageSize = pageSize;
    }

    public int TotalPages => TotalHits > 0 ? (int)Math.Ceiling(TotalHits / (double)_configuredPageSize) : 0;
}
