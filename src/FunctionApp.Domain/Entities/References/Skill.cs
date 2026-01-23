using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Domain.Entities.References;

public class Skill : BaseEntity
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public SkillState? State { get; set; }

    public string? Version { get; set; }
}
