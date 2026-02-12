using Application.Common.Abstractions.Providers;
using Application.Common.Factories;
using Application.Common.Models;
using Application.Contracts.UserDetails;
using Application.UserDetails;


namespace Infrastructure.ExternalServices.Genesys.Providers;

/// <summary>
/// Provides hit count queries for user details from the Genesys Analytics API.
/// </summary>
public sealed class UserDetailsHitCountProvider(IUserDetailsClient client) : IHitCountProvider
{
    /// <summary>
    /// The page size used for hit count queries.
    /// Set to 1 to minimize data transfer since only TotalHits is needed.
    /// </summary>
    private const int HitCountQueryPageSize = 1;

    /// <inheritdoc />
    public async Task<long> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct)
    {
        Interval interval = IntervalFactory.FromDateTimeOffset(start, end);

        // Request with pageSize=1 to minimize data transfer, only care about TotalHits
        UserDetailsResponse response =
            await client.GetUserDetailsAsync(interval.ToString(), 1, HitCountQueryPageSize, ct);

        return response.TotalHits;
    }
}
