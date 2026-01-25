// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Domain.Entities.References;

public class Skill : AuditEntity
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public State? State { get; set; }

    public string? Version { get; set; }
}
