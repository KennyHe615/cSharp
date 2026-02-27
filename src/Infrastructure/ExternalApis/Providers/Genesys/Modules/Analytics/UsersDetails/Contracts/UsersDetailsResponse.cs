namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails.Contracts;

/// <summary>
/// Represents Users Details from the Genesys Analytics API.
/// Contains user presence and routing status history.
/// </summary>
/// <example>
/// {
///     "userDetails": [
///         {
///             "userId": "",
///             "primaryPresence": [
///                 {
///                     "startTime": "2025-08-16T00:05:00.604Z",
///                     "endTime": "2025-08-18T15:58:06.305Z",
///                     "systemPresence": "OFFLINE",
///                     "organizationPresenceId": ""
///                 }
///             ],
///             "routingStatus": [
///                 {
///                     "startTime": "2025-08-16T00:05:01.528Z",
///                     "endTime": "2025-08-18T16:01:01.450Z",
///                     "routingStatus": "OFF_QUEUE"
///                 }
///             ]
///         }
///     ],
///     "totalHits": 70191
/// }
/// </example>
public sealed class UsersDetailsResponse
{
    public List<UserDetailsResponse> UserDetails { get; set; } = [];

    public int TotalHits { get; set; }
}

public sealed class UserDetailsResponse
{
    public Guid UserId { get; set; }

    public List<PrimaryPresenceResponse>? PrimaryPresence { get; set; }

    public List<RoutingStatusResponse>? RoutingStatus { get; set; }
}
