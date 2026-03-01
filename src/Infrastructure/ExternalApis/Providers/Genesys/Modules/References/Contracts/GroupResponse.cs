using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;

/// <summary>
/// Represents a Group from the Genesys API.
/// </summary>
/// <example>
/// {
///     "id": "",
///     "name": "",
///     "description": "",
///     "dateModified": "2023-01-19T15:18:42Z",
///     "memberCount": 2,
///     "state": ${Enum ["active", "inactive", "deleted"]},
///     "version": 3,
///     "type": ${Enum ["official", "social"]},
///     "rulesVisible": true,
///     "visibility": ${Enum ["public", "owners", "members"]},
///     "chat": {
///         "jabberId": ""
///     },
///     "rolesEnabled": true,
///     "includeOwners": true,
///     "owners": [
///         {
///             "id": "",
///             "selfUri": ""
///         }
///     ],
///     "selfUri": ""
/// }
/// </example>
public sealed class GroupResponse
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public int? MemberCount { get; set; }

    public StateKind? State { get; set; }

    public int? Version { get; set; }

    public GroupTypeKind? Type { get; set; }

    public bool? RulesVisible { get; set; }

    public VisibilityKind? Visibility { get; set; }

    public Dictionary<string, string>? Chat { get; set; }

    public bool? RolesEnabled { get; set; }

    public bool? IncludeOwners { get; set; }

    public List<Dictionary<string, string>>? Owners { get; set; }

    public string? SelfUri { get; set; }
}
