using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Application.References.DTOs;

public class PresenceDefinitionDto
{
    public Guid Id { get; set; }

    public Dictionary<string, string>? LanguageLabels { get; set; }

    public SystemPresence? SystemPresence { get; set; }

    public PresenceType? Type { get; set; }

    public bool? Deactivated { get; set; }

    // Currently all is "*" from API response, not GUID
    public string? DivisionId { get; set; }
}
