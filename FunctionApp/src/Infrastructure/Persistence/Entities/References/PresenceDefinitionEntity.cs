using Infrastructure.Genesys.Enums;


namespace Infrastructure.Persistence.Entities.References;

public class PresenceDefinitionEntity : AuditEntity
{
    public Guid Id { get; set; }

    public PresenceType? Type { get; set; }

    public string? LanguageLabel { get; set; }

    public SystemPresence? SystemPresence { get; set; }

    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }
}
