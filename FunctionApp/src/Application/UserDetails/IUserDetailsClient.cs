using Application.Contracts.UserDetails;


namespace Application.UserDetails;

/// <summary>
/// Defines a client for querying user presence and routing status from the Genesys Analytics API.
/// </summary>
public interface IUserDetailsClient
{
    /// <summary>
    /// Queries user details for the specified time interval and page.
    /// </summary>
    /// <param name="queryInterval">
    /// The time interval in ISO 8601 format (e.g., "2026-01-01T00:00Z/2026-01-02T00:00Z").
    /// </param>
    /// <param name="pageNum">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of records per page. If null, uses the default.</param>
    /// <param name="ct">Cancellation token to abort the operation.</param>
    /// <returns>
    /// A response containing user details and the total number of records available.
    /// </returns>
    Task<UserDetailsResponse> GetUserDetailsAsync(string queryInterval,
                                                  int pageNum,
                                                  int? pageSize,
                                                  CancellationToken ct);
}
