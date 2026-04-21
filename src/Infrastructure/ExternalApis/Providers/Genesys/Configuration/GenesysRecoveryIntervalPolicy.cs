using Application.Abstractions.Recovery;

using Microsoft.Extensions.Options;

using SharedKernel.Time;


namespace Infrastructure.ExternalApis.Providers.Genesys.Configuration;

/// <summary>
/// Genesys-backed recovery interval policy using provider retention and future-skew limits.
/// </summary>
public sealed class GenesysRecoveryIntervalPolicy(IOptions<GenesysOptions> options,
                                                  IDateTimeProvider dateTimeProvider) : IRecoveryIntervalPolicy
{
    private readonly GenesysOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private readonly IDateTimeProvider _dateTimeProvider =
                    dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));

    /// <inheritdoc />
    public int HistoricalDataLimitDays => GenesysOptions.HistoricalDataLimitDays;

    /// <inheritdoc />
    public int FutureSkewDays => _options.RecoveryFutureSkewDays;

    /// <inheritdoc />
    public bool IsStartWithinRetention(DateTimeOffset start)
    {
        DateTimeOffset earliestAllowed = _dateTimeProvider.UtcNowOffset.AddDays(-HistoricalDataLimitDays);

        return start >= earliestAllowed;
    }

    /// <inheritdoc />
    public bool IsEndWithinFutureSkew(DateTimeOffset end)
    {
        DateTimeOffset latestAllowed = _dateTimeProvider.UtcNowOffset.AddDays(FutureSkewDays);

        return end <= latestAllowed;
    }
}
