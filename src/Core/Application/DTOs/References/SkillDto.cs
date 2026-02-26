using Application.Enums;


namespace Application.DTOs.References;

public sealed class SkillDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public StateKind State { get; set; }

    public string? Version { get; set; }
}
