using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.Contracts.ExternalApis.Genesys.References;

public sealed class SkillRawContract
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public StateKind? State { get; set; }

    public string? Version { get; set; }

    public string? SelfUri { get; set; }
}
