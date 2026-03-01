using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;

/// <summary>
/// Represents a Presence Definition from the Genesys API.
/// </summary>
/// <example>
/// {
///   "id": "",
///   "type": ${Enum ["User", "System"]},
///   "languageLabels": {
///     "en": "",
///     "en_US": ""
///   },
///   "systemPresence": ${Enum ["Available", "Away", "Busy", "Offline", "Idle", "OnQueue", "Meal", "Training", "Meeting", "Break"]},
///   "divisionId": "*",
///   "deactivated": false,
///   "selfUri": ""
/// }
/// </example>
public sealed class PresenceDefinitionResponse
{
    public Guid Id { get; set; }

    public PresenceTypeKind? Type { get; set; }

    public Dictionary<string, string>? LanguageLabels { get; set; }

    public SystemPresenceKind? SystemPresence { get; set; }

    // Currently all is "*" from API response, not GUID
    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }

    public string? SelfUri { get; set; }
}
