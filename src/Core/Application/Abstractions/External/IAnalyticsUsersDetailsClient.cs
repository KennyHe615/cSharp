using Application.Contracts.ExternalApis.Genesys.UsersDetails;


namespace Application.Abstractions.External;

/// <summary>
/// Application-facing contract for Genesys Analytics Users Details queries.
/// </summary>
public interface IAnalyticsUsersDetailsClient
{
    /// <summary>
    /// Queries Users Details for the provided interval and page.
    /// </summary>
    /// <param name="intervalIso8601">Interval string in ISO-8601 range format
    /// (e.g., "2026-01-01T00:00Z/2026-01-02T00:00Z").</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Optional page size. When null, implementation default is used.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A response containing user details and the total number of records available.
    /// </returns>
    Task<UsersDetailsResponse> GetUsersDetailsAsync(string intervalIso8601,
                                                    int pageNumber,
                                                    int? pageSize = null,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Returns total Users Details hits for the interval.
    /// </summary>
    Task<long> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);
}
