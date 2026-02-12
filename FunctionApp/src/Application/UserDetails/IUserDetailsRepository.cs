using Application.Dtos.UserDetails;


namespace Application.UserDetails;

/// <summary>
/// Defines a repository for persisting user details data.
/// </summary>
public interface IUserDetailsRepository
{
    /// <summary>
    /// Upserts primary presence and routing status data into the database.
    /// </summary>
    /// <param name="primaryPresence">The list of primary presence DTOs to upsert.</param>
    /// <param name="routingStatus">The list of routing status DTOs to upsert.</param>
    /// <param name="ct">The cancellation token to abort the operation.</param>
    /// <returns>A task representing the asynchronous upsert operation.</returns>
    Task UpsertUserDetailsAsync(List<PrimaryPresenceDto> primaryPresence,
                                List<RoutingStatusDto> routingStatus,
                                CancellationToken ct);
}
