using Application.Enums;


namespace Infrastructure.Persistence.Entities.References;

public class Group : Audit
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public int? MemberCount { get; set; }

    public State? State { get; set; }

    public int? Version { get; set; }

    public GroupType? Type { get; set; }

    public bool? RulesVisible { get; set; }

    public GroupVisibility? Visibility { get; set; }

    public string? ChatJabberId { get; set; }

    public bool? RolesEnabled { get; set; }

    public bool? IncludeOwners { get; set; }
}
