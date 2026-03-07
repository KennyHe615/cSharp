using Application.DTOs.UsersDetails;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Defines a repository for persisting user details data.
/// </summary>
public interface IUserDetailsRepository
{
    Task UpsertUserDetailsAsync(IReadOnlyCollection<PrimaryPresenceDto> primaryPresence,
                                IReadOnlyCollection<RoutingStatusDto> routingStatus,
                                CancellationToken ct);
}
