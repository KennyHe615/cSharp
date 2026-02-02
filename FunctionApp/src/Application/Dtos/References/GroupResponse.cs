using Application.Enums;


namespace Application.Dtos.References;

/// <summary>
/// Represents a Group from the Genesys API.
/// </summary>
/// <example>
/// {
///     "id": "62316dbd-8ab6-4cf8-83d7-2dbcc318ca07",
///     "name": "Airlock389CustomerSupportOrg",
///     "description": "Service Desk",
///     "dateModified": "2023-01-19T15:18:42Z",
///     "memberCount": 2,
///     "state": ${Enum ["active", "inactive", "deleted"]},
///     "version": 3,
///     "type": ${Enum ["official", "social"]},
///     "rulesVisible": true,
///     "visibility": ${Enum ["public", "owners", "members"]},
///     "chat": {
///         "jabberId": "63c95f28e95eaf1b56ea76e4@conference.nttm1s.orgspan.com"
///     },
///     "rolesEnabled": true,
///     "includeOwners": true,
///     "owners": [
///         {
///             "id": "5fe9a50b-e419-40cb-9d5a-94828d10630d",
///             "selfUri": "/api/v2/users/5fe9a50b-e419-40cb-9d5a-94828d10630d"
///         }
///     ],
///     "selfUri": "/api/v2/groups/62316dbd-8ab6-4cf8-83d7-2dbcc318ca07"
/// }
/// </example>
public class GroupResponse
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public int? MemberCount { get; set; }

    public State? State { get; set; }

    public int? Version { get; set; }

    public GroupType? Type { get; set; }

    public bool? RulesVisible { get; set; }

    public GroupVisibility? Visibility { get; set; }

    public Dictionary<string, string>? Chat { get; set; }

    public bool? RolesEnabled { get; set; }

    public bool? IncludeOwners { get; set; }
}
