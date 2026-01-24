using FunctionApp.Domain.Enums.References;


namespace FunctionApp.Application.References.DTOs;

public class SkillDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public SkillState? State { get; set; }

    public string? Version { get; set; }
}
