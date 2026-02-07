using Application.Contracts.Enums;


namespace Application.Contracts.References;

/// <summary>
/// Represents a Skill from the Genesys API.
/// </summary>
/// <example>
/// {
///     "id": "b0b7dde8-1fdf-4725-823a-77d4f05bf795",
///     "name": "Assrt_CS_Eng",
///     "dateModified": "2021-07-29T02:52:57Z",
///     "state": ${Enum ["active", "inactive", "deleted"]},
///     "version": "1",
///     "selfUri": "/api/v2/routing/skills/b0b7dde8-1fdf-4725-823a-77d4f05bf795"
/// }
/// </example>
public sealed class SkillResponse
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public State? State { get; set; }

    public string? Version { get; set; }
}
