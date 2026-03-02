using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.DTOs.References;

public sealed class PresenceDefinitionDto
{
    public Guid Id { get; set; }

    public PresenceTypeKind? Type { get; set; }

    public string? LanguageLabel { get; set; }

    public SystemPresenceKind? SystemPresence { get; set; }

    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }
}
