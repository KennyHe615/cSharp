using Infrastructure.ExternalApis.Genesys.Models.Enums;


namespace Infrastructure.Persistence.Entities.References;

public sealed class PresenceDefinition : Audit
{
    public Guid Id { get; set; }

    public PresenceType? Type { get; set; }

    public string? LanguageLabel { get; set; }

    public SystemPresence? SystemPresence { get; set; }

    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }
}
