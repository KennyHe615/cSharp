namespace Application.Contracts.ExternalApis.Genesys.UsersDetails;

/// <summary>
/// Represents Users Details from the Genesys Analytics API.
/// Contains user presence and routing status history.
/// </summary>
/// <example>
/// {
///     "userDetails": [
///         {
///             "userId": "40f5f050-65b7-49e6-8e93-a67aa11fd44d",
///             "primaryPresence": [
///                 {
///                     "startTime": "2025-08-16T00:05:00.604Z",
///                     "endTime": "2025-08-18T15:58:06.305Z",
///                     "systemPresence": "OFFLINE",
///                     "organizationPresenceId": "ccf3c10a-aa2c-4845-8e8d-f59fa48c58e5"
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
