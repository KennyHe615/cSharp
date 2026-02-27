using Infrastructure.ExternalApis.Providers.Genesys.Enums;


namespace Infrastructure.Persistence.Entities.References;

public class Skill : Audit
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public State? State { get; set; }

    public string? Version { get; set; }
}
