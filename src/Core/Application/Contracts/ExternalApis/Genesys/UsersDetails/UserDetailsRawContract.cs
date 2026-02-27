namespace Application.Contracts.ExternalApis.Genesys.UsersDetails;

/// <summary>
/// Provider-agnostic raw users-details payload used by Application boundary.
/// </summary>
public sealed class UsersDetailsRawContract
{
    public List<UserDetailsRawContract> UserDetails { get; set; } = [];

    public int TotalHits { get; set; }
}

public sealed class UserDetailsRawContract
{
    public Guid UserId { get; set; }

    public List<PrimaryPresenceRawContract>? PrimaryPresence { get; set; }

    public List<RoutingStatusRawContract>? RoutingStatus { get; set; }
}
