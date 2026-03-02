using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;

/// <summary>
/// Represents a Skill from the Genesys API.
/// </summary>
/// <example>
/// {
///     "id": "",
///     "name": "",
///     "dateModified": "2021-07-29T02:52:57Z",
///     "state": ${Enum ["active", "inactive", "deleted"]},
///     "version": "1",
///     "selfUri": ""
/// }
/// </example>
public sealed class SkillResponse
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public StateKind? State { get; set; }

    public string? Version { get; set; }

    public string? SelfUri { get; set; }
}
