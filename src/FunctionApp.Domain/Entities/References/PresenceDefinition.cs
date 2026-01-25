// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Domain.Entities.References;

public class PresenceDefinition : AuditEntity
{
    public Guid Id { get; set; }

    public PresenceType? Type { get; set; }

    public string? LanguageLabel { get; set; }

    public SystemPresence? SystemPresence { get; set; }

    public string? DivisionId { get; set; }

    public bool? Deactivated { get; set; }
}
