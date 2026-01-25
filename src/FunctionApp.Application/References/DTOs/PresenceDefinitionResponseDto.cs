using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Application.References.DTOs;

/// <summary>
/// Represents a Presence Definition from the Genesys API.
/// </summary>
/// <example>
/// {
///   "id": "015f5de5-cb0b-47a7-80ae-7410bb9b3dff",
///   "type": ${Enum ["User", "System"]},
///   "languageLabels": {
///     "en": "Chat",
///     "en_US": "Chat"
///   },
///   "systemPresence": ${Enum ["Available", "Away", "Busy", "Offline", "Idle", "OnQueue", "Meal", "Training", "Meeting", "Break"]},
///   "divisionId": "*",
///   "deactivated": false,
///   "selfUri": "/api/v2/presence/definitions/015f5de5-cb0b-47a7-80ae-7410bb9b3dff"
/// }
/// </example>
public class PresenceDefinitionResponseDto
{
    public Guid Id { get; set; }

    public PresenceType? Type { get; set; }

    public Dictionary<string, string>? LanguageLabels { get; set; }

    public SystemPresence? SystemPresence { get; set; }

    // Currently all is "*" from API response, not GUID
    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }
}
