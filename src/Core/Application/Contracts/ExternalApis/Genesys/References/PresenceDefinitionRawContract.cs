using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.Contracts.ExternalApis.Genesys.References;

public sealed class PresenceDefinitionRawContract
{
    public Guid Id { get; set; }

    public PresenceTypeKind? Type { get; set; }

    public Dictionary<string, string>? LanguageLabels { get; set; }

    public SystemPresenceKind? SystemPresence { get; set; }

    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }

    public string? SelfUri { get; set; }
}
