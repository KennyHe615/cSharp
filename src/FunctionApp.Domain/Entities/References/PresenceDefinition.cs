using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Domain.Entities.References;

public class PresenceDefinition : BaseEntity
{
    public Guid Id { get; set; }

    public string? LanguageLabel { get; set; }

    public SystemPresence? SystemPresence { get; set; }

    public PresenceType? Type { get; set; }

    public bool? Deactivated { get; set; }

    public string? DivisionId { get; set; }
}
